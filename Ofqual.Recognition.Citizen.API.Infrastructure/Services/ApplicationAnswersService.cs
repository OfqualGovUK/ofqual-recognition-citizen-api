using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.ApplicationAnswers;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class ApplicationAnswersService : IApplicationAnswersService
{
    private readonly IUnitOfWork _context;
    private readonly IUserInformationService _userInformationService;

    public ApplicationAnswersService(IUnitOfWork context, IUserInformationService userInformationService)
    {
        _context = context;
        _userInformationService = userInformationService;
    }

    public async Task<bool> SubmitAnswerAndUpdateStatus(Guid applicationId, Guid taskId, Guid questionId, string answerJson)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        bool isAnswerUpserted = await _context.ApplicationAnswersRepository.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn);
        if (!isAnswerUpserted)
        {
            return false;
        }

        bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress, upn);
        if (!isStatusUpdated)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> SavePreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        foreach (var answer in answers)
        {
            ValidationResponse? validationResult = await ValidateQuestionAnswers(answer.QuestionId, answer.AnswerJson);
            if (validationResult == null || (validationResult.Errors != null && validationResult.Errors.Any()))
            {
                return false;
            }

            bool success = await _context.ApplicationAnswersRepository.UpsertQuestionAnswer(applicationId, answer.QuestionId, answer.AnswerJson, upn);
            if (!success)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<List<TaskReviewSectionDto>> GetAllApplicationAnswerReview(Guid applicationId)
    {
        var allAnswers = await _context.ApplicationAnswersRepository.GetAllApplicationAnswers(applicationId);
        if (allAnswers == null || !allAnswers.Any())
        {
            return new List<TaskReviewSectionDto>();
        }

        var groupedBySection = allAnswers
            .GroupBy(a => new { a.SectionId, a.SectionName, a.SectionOrderNumber })
            .OrderBy(g => g.Key.SectionOrderNumber);

        var result = new List<TaskReviewSectionDto>();

        foreach (var sectionGroup in groupedBySection)
        {
            var tasks = sectionGroup
                .GroupBy(q => new { q.TaskId, q.TaskOrderNumber })
                .OrderBy(g => g.Key.TaskOrderNumber);

            var sectionTasks = new List<TaskReviewGroupDto>();

            foreach (var taskGroup in tasks)
            {
                var taskAnswers = await GetTaskAnswerReview(applicationId, taskGroup.Key.TaskId);
                sectionTasks.AddRange(taskAnswers);
            }

            result.Add(new TaskReviewSectionDto
            {
                SectionName = sectionGroup.Key.SectionName,
                TaskGroups = sectionTasks
            });
        }

        return result;
    }

    public async Task<List<TaskReviewGroupDto>> GetTaskAnswerReview(Guid applicationId, Guid taskId)
    {
        var taskQuestionAnswers = await _context.ApplicationAnswersRepository.GetTaskQuestionAnswers(applicationId, taskId);
        if (!taskQuestionAnswers.Any())
        {
            return new List<TaskReviewGroupDto>();
        }

        var sections = new List<TaskReviewGroupDto>();

        foreach (var question in taskQuestionAnswers)
        {
            var section = await ProcessQuestionAnswer(applicationId, question);
            if (section != null && section.QuestionAnswers.Any())
            {
                sections.Add(section);
            }
        }

        return sections;
    }

    private async Task<TaskReviewGroupDto?> ProcessQuestionAnswer(Guid applicationId, SectionTaskQuestionAnswer question)
    {
        if (string.IsNullOrWhiteSpace(question.QuestionContent))
        {
            return null;
        }

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(question.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
        if (questionContent?.FormGroup == null)
        {
            return null;
        }

        var answerValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(question.Answer, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var formGroup = questionContent.FormGroup;
        var questionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}";

        var sectionGroups = new ISectionGroup?[]
        {
            formGroup.TextInputGroup,
            formGroup.Textarea,
            formGroup.RadioButtonGroup,
            formGroup.CheckboxGroup,
            formGroup.FileUpload
        };

        var section = new TaskReviewGroupDto
        {
            SectionHeading = sectionGroups.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s?.SectionName))?.SectionName
        };

        // Text input group
        if (formGroup.TextInputGroup?.Fields != null)
        {
            foreach (var input in formGroup.TextInputGroup.Fields)
            {
                var values = ExtractAnswer(answerValues, input.Name);
                section.QuestionAnswers.Add(new TaskReviewItemDto
                {
                    QuestionText = input.Label,
                    AnswerValue = values,
                    QuestionUrl = questionUrl
                });
            }
        }

        // Textarea
        if (formGroup.Textarea != null)
        {
            var values = ExtractAnswer(answerValues, formGroup.Textarea.Name);
            section.QuestionAnswers.Add(new TaskReviewItemDto
            {
                QuestionText = formGroup.Textarea.Label?.Text,
                AnswerValue = values,
                QuestionUrl = questionUrl
            });
        }

        // Radio button group
        if (formGroup.RadioButtonGroup != null)
        {
            var values = ExtractAnswer(answerValues, formGroup.RadioButtonGroup.Name);
            section.QuestionAnswers.Add(new TaskReviewItemDto
            {
                QuestionText = formGroup.RadioButtonGroup.Heading?.Text,
                AnswerValue = values,
                QuestionUrl = questionUrl
            });

            var selected = values.FirstOrDefault()?.ToLowerInvariant();
            var selectedOption = formGroup.RadioButtonGroup.Options.FirstOrDefault(o => o.Value.ToLowerInvariant() == selected);
            if (selectedOption != null)
            {
                if (selectedOption.ConditionalInputs != null)
                {
                    foreach (var input in selectedOption.ConditionalInputs)
                    {
                        var conditionalValues = ExtractAnswer(answerValues, input.Name);
                        section.QuestionAnswers.Add(new TaskReviewItemDto
                        {
                            QuestionText = input.Label,
                            AnswerValue = conditionalValues,
                            QuestionUrl = questionUrl
                        });
                    }
                }

                if (selectedOption.ConditionalSelects != null)
                {
                    foreach (var select in selectedOption.ConditionalSelects)
                    {
                        var conditionalValues = ExtractAnswer(answerValues, select.Name);
                        section.QuestionAnswers.Add(new TaskReviewItemDto
                        {
                            QuestionText = select.Label,
                            AnswerValue = conditionalValues,
                            QuestionUrl = questionUrl
                        });
                    }
                }
            }
        }

        // Checkbox group
        if (formGroup.CheckboxGroup != null)
        {
            var checkbox = formGroup.CheckboxGroup;
            var values = ExtractAnswer(answerValues, checkbox.Name);
            var selected = values.Select(v => v.ToLowerInvariant()).ToHashSet();

            section.QuestionAnswers.Add(new TaskReviewItemDto
            {
                QuestionText = checkbox.Heading?.Text,
                AnswerValue = values,
                QuestionUrl = questionUrl
            });

            foreach (var option in checkbox.Options)
            {
                if (string.IsNullOrWhiteSpace(option.Value) || !selected.Contains(option.Value.ToLowerInvariant()))
                {
                    continue;
                }

                if (option.ConditionalInputs != null)
                {
                    foreach (var input in option.ConditionalInputs)
                    {
                        var conditionalValues = ExtractAnswer(answerValues, input.Name);
                        section.QuestionAnswers.Add(new TaskReviewItemDto
                        {
                            QuestionText = input.Label,
                            AnswerValue = conditionalValues,
                            QuestionUrl = questionUrl
                        });
                    }
                }

                if (option.ConditionalSelects != null)
                {
                    foreach (var select in option.ConditionalSelects)
                    {
                        var conditionalValues = ExtractAnswer(answerValues, select.Name);
                        section.QuestionAnswers.Add(new TaskReviewItemDto
                        {
                            QuestionText = select.Label,
                            AnswerValue = conditionalValues,
                            QuestionUrl = questionUrl
                        });
                    }
                }
            }
        }

        // File upload
        if (formGroup.FileUpload != null)
        {
            var allAttachments = await _context.AttachmentRepository.GetAllAttachmentsForLink(applicationId, question.QuestionId, LinkType.Question);

            var sortedFileNames = allAttachments
                .Select(a => a.FileName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!sortedFileNames.Any())
            {
                sortedFileNames.Add("Not provided");
            }

            section.QuestionAnswers.Add(new TaskReviewItemDto
            {
                QuestionText = "Files you uploaded",
                AnswerValue = sortedFileNames,
                QuestionUrl = questionUrl
            });
        }

        return section.QuestionAnswers.Any() ? section : null;
    }

    private static List<string> ExtractAnswer(Dictionary<string, JsonElement>? answers, string key)
    {
        if (answers == null || !answers.TryGetValue(key, out var token))
        {
            return new List<string> { "Not provided" };
        }

        if (token.ValueKind == JsonValueKind.Array)
        {
            return token.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToList();
        }

        if (token.ValueKind == JsonValueKind.String)
        {
            var str = token.GetString();
            return string.IsNullOrWhiteSpace(str) ? new List<string> { "Not provided" } : new List<string> { str };
        }

        return new List<string> { token.ToString() };
    }

    public async Task<ValidationResponse?> ValidateQuestionAnswers(Guid questionId, string answerJson)
    {
        var questionDetails = await _context.QuestionRepository.GetQuestionByQuestionId(questionId);
        if (questionDetails == null)
        {
            return null;
        }

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
        if (questionContent?.FormGroup == null)
        {
            return null;
        }

        var answerValue = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (answerValue == null)
        {
            return null;
        }

        var errors = new List<ValidationErrorItem>();
        var components = new List<IValidatable>();
        var selectedCheckboxValues = new List<string>();
        var selectedRadioValue = string.Empty;
        var formGroup = questionContent.FormGroup;

        if (formGroup.Textarea != null)
        {
            components.Add(formGroup.Textarea);
        }

        if (formGroup.TextInputGroup?.Fields != null)
        {
            components.AddRange(formGroup.TextInputGroup.Fields);
        }

        if (formGroup.CheckboxGroup != null)
        {
            components.Add(formGroup.CheckboxGroup);

            foreach (var option in formGroup.CheckboxGroup.Options)
            {
                if (option.ConditionalInputs != null)
                {
                    components.AddRange(option.ConditionalInputs);
                }

                if (option.ConditionalSelects != null)
                {
                    components.AddRange(option.ConditionalSelects);
                }
            }

            if (formGroup.CheckboxGroup.Name != null &&
                answerValue.TryGetValue(formGroup.CheckboxGroup.Name, out var checkboxAnswerElement))
            {
                if (checkboxAnswerElement.ValueKind == JsonValueKind.Array)
                {
                    selectedCheckboxValues = checkboxAnswerElement.EnumerateArray()
                        .Select(x => x.GetString()?.Trim().ToLowerInvariant())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()!;
                }
                else if (checkboxAnswerElement.ValueKind == JsonValueKind.String)
                {
                    var val = checkboxAnswerElement.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        selectedCheckboxValues.Add(val.Trim().ToLowerInvariant());
                    }
                }
            }
        }

        if (formGroup.RadioButtonGroup != null)
        {
            components.Add(formGroup.RadioButtonGroup);

            if (answerValue.TryGetValue(formGroup.RadioButtonGroup.Name, out var radioAnswerElement) &&
                radioAnswerElement.ValueKind == JsonValueKind.String)
            {
                selectedRadioValue = radioAnswerElement.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;

                var selectedOption = formGroup.RadioButtonGroup.Options
                    .FirstOrDefault(opt => opt.Value.Trim().ToLowerInvariant() == selectedRadioValue);

                if (selectedOption != null)
                {
                    if (selectedOption.ConditionalInputs != null)
                    {
                        components.AddRange(selectedOption.ConditionalInputs);
                    }

                    if (selectedOption.ConditionalSelects != null)
                    {
                        components.AddRange(selectedOption.ConditionalSelects);
                    }
                }
            }
        }

        foreach (var component in components)
        {
            var validation = component.Validation;
            if (validation == null)
            {
                continue;
            }

            if (formGroup.CheckboxGroup?.Options != null)
            {
                var parent = formGroup.CheckboxGroup.Options.FirstOrDefault(cb =>
                    (cb.ConditionalInputs?.Any(i => i.Name == component.Name) ?? false) ||
                    (cb.ConditionalSelects?.Any(s => s.Name == component.Name) ?? false));

                if (parent != null && !string.IsNullOrWhiteSpace(parent.Value))
                {
                    if (!selectedCheckboxValues.Contains(parent.Value.Trim().ToLowerInvariant()))
                    {
                        continue;
                    }
                }
            }

            if (formGroup.RadioButtonGroup?.Options != null)
            {
                var parent = formGroup.RadioButtonGroup.Options.FirstOrDefault(rb =>
                    (rb.ConditionalInputs?.Any(i => i.Name == component.Name) ?? false) ||
                    (rb.ConditionalSelects?.Any(s => s.Name == component.Name) ?? false));

                if (parent != null && !string.IsNullOrWhiteSpace(parent.Value))
                {
                    if (!string.Equals(parent.Value.Trim().ToLowerInvariant(), selectedRadioValue))
                    {
                        continue;
                    }
                }
            }

            if (!answerValue.TryGetValue(component.Name, out var answerElement))
            {
                if (validation.Required == true)
                {
                    var label = StringHelper.CapitaliseFirstLetter(component.ValidationLabel);

                    string message = component switch
                    {
                        CheckBoxGroup => $"Select at least one option for {label}",
                        RadioButtonGroup => $"Select an option for {label}",
                        _ => $"Enter {label}"
                    };

                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = message
                    });
                }

                continue;
            }

            string? answerString = null;
            List<string>? answerArray = null;

            if (answerElement.ValueKind == JsonValueKind.String)
            {
                answerString = answerElement.GetString();
            }
            else if (answerElement.ValueKind == JsonValueKind.Array)
            {
                answerArray = answerElement.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()!;
                answerString = string.Join(", ", answerArray);
            }

            if (string.IsNullOrWhiteSpace(answerString) && (answerArray == null || !answerArray.Any()))
            {
                if (validation.Required == true)
                {
                    var label = StringHelper.CapitaliseFirstLetter(component.ValidationLabel);

                    string message = component switch
                    {
                        CheckBoxGroup => $"Select at least one option for {label}",
                        RadioButtonGroup => $"Select an option for {label}",
                        _ => $"Enter {label}"
                    };

                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = message
                    });
                }

                continue;
            }

            if (validation.Unique == true && !string.IsNullOrWhiteSpace(answerString))
            {
                var exists = await _context.ApplicationAnswersRepository.CheckIfQuestionAnswerExists(questionId, component.Name, answerString);
                if (exists)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"The {StringHelper.CapitaliseFirstLetter(component.ValidationLabel)} \"{answerString}\" already exists in our records"
                    });
                }

                continue;
            }

            if ((validation.MinLength.HasValue || validation.MaxLength.HasValue) && !string.IsNullOrWhiteSpace(answerString))
            {
                bool countWords = validation.CountWords == true;

                if (countWords)
                {
                    int length = answerString.Split(['\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).Length;

                    if (validation.MinLength.HasValue && length < validation.MinLength.Value)
                    {
                        errors.Add(new ValidationErrorItem
                        {
                            PropertyName = component.Name,
                            ErrorMessage = $"{StringHelper.CapitaliseFirstLetter(component.ValidationLabel)} must be {validation.MinLength.Value} words or more"
                        });

                        continue;
                    }

                    if (validation.MaxLength.HasValue && length > validation.MaxLength.Value)
                    {
                        errors.Add(new ValidationErrorItem
                        {
                            PropertyName = component.Name,
                            ErrorMessage = $"{StringHelper.CapitaliseFirstLetter(component.ValidationLabel)} must be {validation.MaxLength.Value} words or fewer"
                        });

                        continue;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(validation.Pattern) && !string.IsNullOrWhiteSpace(answerString))
            {
                var regex = new Regex(validation.Pattern);
                if (!regex.IsMatch(answerString))
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"Enter a valid {StringHelper.CapitaliseFirstLetter(component.ValidationLabel)}"
                    });

                    continue;
                }
            }

            if (validation.MinSelected.HasValue || validation.MaxSelected.HasValue)
            {
                int selectedCount = 0;

                if (answerElement.ValueKind == JsonValueKind.Array)
                {
                    selectedCount = answerElement.EnumerateArray()
                        .Count(x => !string.IsNullOrWhiteSpace(x.GetString()));
                }
                else if (answerElement.ValueKind == JsonValueKind.String)
                {
                    var val = answerElement.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        selectedCount = 1;
                    }
                }

                if (validation.MinSelected.HasValue && selectedCount < validation.MinSelected.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"Select at least {validation.MinSelected.Value} option{(validation.MinSelected.Value > 1 ? "s" : "")} for {StringHelper.CapitaliseFirstLetter(component.ValidationLabel)}"
                    });

                    continue;
                }

                if (validation.MaxSelected.HasValue && selectedCount > validation.MaxSelected.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"You can only select up to {validation.MaxSelected.Value} option{(validation.MaxSelected.Value > 1 ? "s" : "")} for {StringHelper.CapitaliseFirstLetter(component.ValidationLabel)}"
                    });

                    continue;
                }
            }
        }

        return new ValidationResponse { Errors = errors };
    }
}