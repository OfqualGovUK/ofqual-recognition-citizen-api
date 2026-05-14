using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class ApplicationAnswersService : IApplicationAnswersService
{
    private readonly IUnitOfWork _context;
    private readonly IUserInformationService _userInformationService;
    private readonly IStageService _stageService;

    private static readonly string[] _fieldsToCompareInPortal = ["OrganisationId", "OrganisationName", "Acronym"];

    public ApplicationAnswersService(IUnitOfWork context, IUserInformationService userInformationService, IStageService stageService)
    {
        _context = context;
        _userInformationService = userInformationService;
        _stageService = stageService;
    }

    public async Task<bool> SubmitAnswerAndUpdateStatus(Guid applicationId, Guid taskId, Guid questionId, string answerJson)
    {
        string upn = _userInformationService.GetCurrentUserUpn();

        bool isAnswerUpserted = await _context.ApplicationAnswersRepository.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn);
        if (!isAnswerUpserted)
        {
            return false;
        }

        bool isStatusUpdated = await _context.TaskStatusRepository.UpdateTaskStatus(applicationId, taskId, StatusType.InProgress, upn);
        if (!isStatusUpdated)
        {
            return false;
        }

        bool stageStatusUpdated = await _stageService.EvaluateAndUpsertAllStageStatus(applicationId);
        if (!stageStatusUpdated)
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
            ValidationResponse? validationResult = await ValidateQuestionAnswers(answer.QuestionId, answer.AnswerJson, applicationId);
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
            return [];
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
                var taskName = taskGroup.FirstOrDefault()?.TaskName;

                foreach (var group in taskAnswers)
                {
                    if (string.IsNullOrWhiteSpace(group.SectionHeading))
                    {
                        group.SectionHeading = taskName;
                    }

                    var existingGroup = sectionTasks.FirstOrDefault(t => t.SectionHeading == group.SectionHeading);
                    if (existingGroup != null)
                    {
                        existingGroup.QuestionAnswers.AddRange(group.QuestionAnswers);
                    }
                    else
                    {
                        sectionTasks.Add(group);
                    }
                }
            }

            if (sectionGroup.Any(reviewTask => reviewTask.ReviewFlag))
            {
                result.Add(new TaskReviewSectionDto
                {
                    SectionName = sectionGroup.Key.SectionName,
                    TaskGroups = sectionTasks
                });
            }
        }

        return result;
    }

    public async Task<List<TaskReviewGroupDto>> GetTaskAnswerReview(Guid applicationId, Guid taskId)
    {
        var taskQuestionAnswers = await _context.ApplicationAnswersRepository.GetTaskQuestionAnswers(applicationId, taskId);
        if (!taskQuestionAnswers.Any())
        {
            return [];
        }

        var sections = new List<TaskReviewGroupDto>();

        foreach (var question in taskQuestionAnswers)
        {
            var section = await ProcessQuestionAnswer(applicationId, question);
            if (section != null && section.QuestionAnswers.Count != 0)
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

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(question.QuestionContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
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
            var selectedOption = formGroup.RadioButtonGroup.Options.FirstOrDefault(o => o.Value.Equals(selected, StringComparison.InvariantCultureIgnoreCase));
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

            if (sortedFileNames.Count == 0)
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

        return section.QuestionAnswers.Count != 0 ? section : null;
    }

    private static List<string> ExtractAnswer(Dictionary<string, JsonElement>? answers, string key)
    {
        if (answers == null || !answers.TryGetValue(key, out var token))
        {
            return ["Not provided"];
        }

        if (token.ValueKind == JsonValueKind.Array)
        {
            return [.. token.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())];
        }

        if (token.ValueKind == JsonValueKind.String)
        {
            var str = token.GetString();
            return string.IsNullOrWhiteSpace(str) ? ["Not provided"] : [str];
        }

        return [token.ToString()];
    }

    public async Task<ValidationResponse?> ValidateQuestionAnswers(Guid questionId, string answerJson, Guid? applicationId = null)
    {
        var questionDetails = await _context.QuestionRepository.GetQuestionByQuestionId(questionId);
        if (questionDetails == null)
        {
            return null;
        }

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
        if (questionContent?.FormGroup == null)
        {
            return null;
        }

        var answerValue = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answerJson, options: 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
                    .FirstOrDefault(opt => opt.Value.Trim().Equals(selectedRadioValue, StringComparison.InvariantCultureIgnoreCase));

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

            //If compoent is a parent, skip validating it as we should validate children
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

            //If compoent is a parent, skip validating it as we should validate children
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

            //if validation has a defined componentValidationLabel, use it, otherwise use the component name
            var componentValidationLabel = component.GetValidationLabel();

            if (!answerValue.TryGetValue(component.Name, out var answerElement))
            {
                if (validation.Required == true)
                {
                    string message = component switch
                    {
                        CheckBoxGroup or RadioButtonGroup => $"Select {componentValidationLabel}",
                        _ => $"Enter {componentValidationLabel}"
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

            //Check if the answer is empty or null when required
            if (string.IsNullOrWhiteSpace(answerString) && (answerArray == null || answerArray.Count == 0))
            {
                if (validation.Required == true)
                {
                    string message = component switch
                    {
                        CheckBoxGroup or RadioButtonGroup => $"Select {componentValidationLabel}",
                        _ => $"Enter {componentValidationLabel}"
                    };

                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = message
                    });
                }

                continue;
            }

            //Following validation is only applicable for CheckBoxGroup and RadioButtonGroup
            if (component is CheckBoxGroup or RadioButtonGroup)
            {
                // Check for minimum and maximum selected options where applicable
                var selectError = ValidateSelectableComponent(component, validation, answerElement);
                if (selectError != null)
                {
                    errors.Add(selectError);
                }

                // following validations are not applicable for CheckBoxGroup and RadioButtonGroup
                continue;
            }

            //Check if the answer is in database if unique validation is required
            if (validation.Unique == true && !string.IsNullOrWhiteSpace(answerString))
            {
                //if its a "special" field that needs to be compared with portal, then check if the answer exists in portal,
                //if yes return the specific error message, if no continue with normal unique validation
                if (_fieldsToCompareInPortal.Contains(component.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (await _context.ApplicationAnswersRepository.CheckIfOrganisationExistsInPortal(component.Name, answerString))
                    {
                        /*  If the answer exists in the portal, we will need to stop the user, there and then, and ask them to contact Ofqual,
                            Hence we just return the error message for the first field and ignore all other errors.   
                        */
                        return new ValidationResponse
                        {
                            Errors = [ new ValidationErrorItem
                                {
                                    PropertyName = string.Empty, //this will allow the error to display without a field being highlighted
                                    ErrorMessage = "A previous application appears to have been made on our existing system, "+
                                                   "You will need to contact Ofqual to continue with your application."
                                }]
                        };
                    }                    
                }
                
                if (await _context.ApplicationAnswersRepository.CheckIfQuestionAnswerExists(questionId, component.Name, answerString, applicationId))
                {   
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"The {componentValidationLabel} \"{answerString}\" already exists in our records"
                    });
                    continue;
                }               
            }

            // Check for minimum and maximum length or word count
            if ((validation.MinLength.HasValue || validation.MaxLength.HasValue) && !string.IsNullOrWhiteSpace(answerString))
            {
                bool countWords = validation.CountWords == true;

                int length = countWords
                   ? answerString.Split(['\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).Length
                   : answerString.Length;

                string countType = countWords ? "words" : "characters";

                if (validation.MinLength.HasValue && length < validation.MinLength.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"{componentValidationLabel} must be {validation.MinLength.Value} {countType} or more"
                    });

                    continue;
                }

                if (validation.MaxLength.HasValue && length > validation.MaxLength.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name, 
                        ErrorMessage = $"{componentValidationLabel} must be {validation.MaxLength.Value} {countType} or fewer"
                    });

                    continue;
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
                        ErrorMessage = $"Enter a valid {componentValidationLabel}"
                    });

                    continue;
                }
            }
        }
        return new ValidationResponse { Errors = errors };
    }

    private static ValidationErrorItem? ValidateSelectableComponent(IValidatable component, ValidationRule validation, JsonElement answerElement)
    {
        if (validation.MinSelected.HasValue || validation.MaxSelected.HasValue)
        {
            int selectedCount = 0;

            if (answerElement.ValueKind == JsonValueKind.Array)
            {
                selectedCount = answerElement.EnumerateArray().Count(x => !string.IsNullOrWhiteSpace(x.GetString()));
            }
            else if (answerElement.ValueKind == JsonValueKind.String)
            {
                var val = answerElement.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    selectedCount = 1;
                }
            }

            var componentValidationLabel = component.GetValidationLabel();

            if (validation.MinSelected.HasValue && selectedCount < validation.MinSelected.Value)
            {
                var countText = validation.MinSelected.Value > 1
                    ? $"{validation.MinSelected.Value} options"
                    : "1 option";

                return new ValidationErrorItem
                {
                    PropertyName = component.Name,
                    ErrorMessage = $"Select at least {countText} for {componentValidationLabel}"
                };
            }

            if (validation.MaxSelected.HasValue && selectedCount > validation.MaxSelected.Value)
            {
                var countText = validation.MaxSelected.Value > 1
                    ? $"{validation.MaxSelected.Value} options"
                    : "1 option";

                return new ValidationErrorItem
                {
                    PropertyName = component.Name,
                    ErrorMessage = $"You can only select up to {countText} for {componentValidationLabel}"
                };
            }
        }
        return null;
    }
}