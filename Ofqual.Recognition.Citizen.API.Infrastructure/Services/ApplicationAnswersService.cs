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

    public ApplicationAnswersService(IUnitOfWork context)
    {
        _context = context;
    }

    public async Task<bool> SavePreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers)
    {
        foreach (var answer in answers)
        {
            ValidationResponse? validationResult = await ValidateQuestionAnswers(answer.QuestionId, answer.AnswerJson);
            if (validationResult == null || (validationResult.Errors != null && validationResult.Errors.Any()))
            {
                return false;
            }

            bool success = await _context.ApplicationAnswersRepository.UpsertQuestionAnswer(applicationId, answer.QuestionId, answer.AnswerJson);
            if (!success)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<List<QuestionAnswerSectionDto>> GetTaskAnswerReview(Guid applicationId, Guid taskId)
    {
        var taskQuestionAnswers = await _context.ApplicationAnswersRepository.GetTaskQuestionAnswers(applicationId, taskId);
        if (!taskQuestionAnswers.Any())
        {
            return new List<QuestionAnswerSectionDto>();
        }

        var sections = new List<QuestionAnswerSectionDto>();

        foreach (var question in taskQuestionAnswers)
        {
            if (string.IsNullOrWhiteSpace(question.Answer) || string.IsNullOrWhiteSpace(question.QuestionContent))
            {
                continue;
            }

            var questionContent = JsonSerializer.Deserialize<QuestionContent>(question.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } });
            if (questionContent?.FormGroup == null)
            {
                continue;
            }

            var answerValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(question.Answer, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (answerValues == null)
            {
                continue;
            }

            var formGroup = questionContent.FormGroup;

            // Text inputs
            if (formGroup.TextInputGroup?.Fields != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.TextInputGroup.SectionName
                };

                foreach (var input in formGroup.TextInputGroup.Fields)
                {
                    var values = ExtractAnswer(answerValues, input.Name);
                    section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                    {
                        QuestionText = input.Label,
                        AnswerValue = values,
                        QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                    });
                }

                if (section.QuestionAnswers.Any())
                {
                    sections.Add(section);
                }
            }

            // Textarea
            if (formGroup.Textarea != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.Textarea.SectionName
                };

                var values = ExtractAnswer(answerValues, formGroup.Textarea.Name);
                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = formGroup.Textarea.Label?.Text,
                    AnswerValue = values,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                });

                sections.Add(section);
            }

            // Radio button group
            if (formGroup.RadioButtonGroup != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.RadioButtonGroup.SectionName
                };

                var values = ExtractAnswer(answerValues, formGroup.RadioButtonGroup.Name);
                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = formGroup.RadioButtonGroup.Heading?.Text,
                    AnswerValue = values,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
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
                            section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                            {
                                QuestionText = input.Label,
                                AnswerValue = conditionalValues,
                                QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                            });
                        }
                    }

                    if (selectedOption.ConditionalSelects != null)
                    {
                        foreach (var select in selectedOption.ConditionalSelects)
                        {
                            var conditionalValues = ExtractAnswer(answerValues, select.Name);
                            section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                            {
                                QuestionText = select.Label,
                                AnswerValue = conditionalValues,
                                QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                            });
                        }
                    }
                }

                if (section.QuestionAnswers.Any())
                {
                    sections.Add(section);
                }
            }

            // Checkbox group
            if (formGroup.CheckboxGroup != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.CheckboxGroup.SectionName
                };

                var checkbox = formGroup.CheckboxGroup;
                var values = ExtractAnswer(answerValues, checkbox.Name);
                var selected = values.Select(v => v.ToLowerInvariant()).ToHashSet();

                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = checkbox.Heading?.Text,
                    AnswerValue = values,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
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
                            section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                            {
                                QuestionText = input.Label,
                                AnswerValue = conditionalValues,
                                QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                            });
                        }
                    }

                    if (option.ConditionalSelects != null)
                    {
                        foreach (var select in option.ConditionalSelects)
                        {
                            var conditionalValues = ExtractAnswer(answerValues, select.Name);
                            section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                            {
                                QuestionText = select.Label,
                                AnswerValue = conditionalValues,
                                QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                            });
                        }
                    }
                }

                if (section.QuestionAnswers.Any())
                {
                    sections.Add(section);
                }
            }

            // File Upload
            if (formGroup.FileUpload != null)
            {
                var allAttachments = await _context.AttachmentRepository.GetAllAttachmentsForLink(applicationId, question.QuestionId, LinkType.Question);

                var sortedFileNames = allAttachments
                    .Select(a => a.FileName)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.FileUpload.SectionName
                };

                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = "Files you uploaded",
                    AnswerValue = sortedFileNames,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                });

                sections.Add(section);
            }
        }

        return sections;
    }

    private static List<string> ExtractAnswer(Dictionary<string, JsonElement> answers, string key)
    {
        if (!answers.TryGetValue(key, out var token))
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