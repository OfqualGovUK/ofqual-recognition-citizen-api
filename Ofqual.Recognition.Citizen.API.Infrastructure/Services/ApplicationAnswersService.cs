using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Models;
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
            ValidationResponse validationResult = await ValidateQuestionAnswers(answer.QuestionId, answer.AnswerJson);
            if (!string.IsNullOrEmpty(validationResult.Message) || (validationResult.Errors != null && validationResult.Errors.Any()))
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

            var content = JsonSerializer.Deserialize<QuestionContent>(question.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (content?.FormGroup == null)
            {
                continue;
            }

            var answerValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(question.Answer, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (answerValues == null)
            {
                continue;
            }

            var formGroup = content.FormGroup;

            // Text inputs
            if (formGroup.TextInput?.TextInputs != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.TextInput.SectionName
                };

                foreach (var input in formGroup.TextInput.TextInputs)
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

            // Radio button
            if (formGroup.RadioButton != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.RadioButton.SectionName
                };

                var values = ExtractAnswer(answerValues, formGroup.RadioButton.Name);

                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = formGroup.RadioButton.Heading?.Text,
                    AnswerValue = values,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                });

                sections.Add(section);
            }

            // Checkbox and conditionals
            if (formGroup.CheckBox != null)
            {
                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = formGroup.CheckBox.SectionName
                };

                var checkbox = formGroup.CheckBox;
                var values = ExtractAnswer(answerValues, checkbox.Name);
                var selected = values.Select(v => v.ToLowerInvariant()).ToHashSet();

                section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = checkbox.Heading?.Text,
                    AnswerValue = values,
                    QuestionUrl = $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                });

                foreach (var option in checkbox.CheckBoxes)
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

    public async Task<ValidationResponse> ValidateQuestionAnswers(Guid questionId, string answerJson)
    {
        QuestionDetails? questionDetails = await _context.QuestionRepository.GetQuestionByQuestionId(questionId);
        if (questionDetails == null)
        {
            return new ValidationResponse { Message = "We could not check your answer. Please try again." };
        }

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (questionContent?.FormGroup == null)
        {
            return new ValidationResponse { Message = "We could not check your answer. Please try again." };
        }

        var answerValue = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answerJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (answerValue == null)
        {
            return new ValidationResponse { Message = "We could not check your answer. Please try again." };
        }

        var errors = new List<ValidationErrorItem>();
        var components = new List<IValidatable>();
        var formGroup = questionContent.FormGroup;
        var selectedCheckboxValues = new List<string>();

        if (formGroup.Textarea != null) components.Add(formGroup.Textarea);
        if (formGroup.RadioButton != null) components.Add(formGroup.RadioButton);
        if (formGroup.TextInput != null) components.AddRange(formGroup.TextInput.TextInputs);
        if (formGroup.CheckBox != null)
        {
            components.Add(formGroup.CheckBox);
            if (formGroup.CheckBox.CheckBoxes != null)
            {
                components.AddRange(formGroup.CheckBox.CheckBoxes
                    .SelectMany(x => x.ConditionalInputs ?? new List<TextInputItem>()));

                components.AddRange(formGroup.CheckBox.CheckBoxes
                    .SelectMany(x => x.ConditionalSelects ?? new List<Select>()));
            }

            if (formGroup.CheckBox?.Name != null && answerValue.TryGetValue(formGroup.CheckBox.Name, out var checkboxAnswerElement))
            {
                if (checkboxAnswerElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in checkboxAnswerElement.EnumerateArray())
                    {
                        var val = item.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            selectedCheckboxValues.Add(val.Trim().ToLowerInvariant());
                        }
                    }
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

        foreach (var component in components)
        {
            var validation = component.Validation;
            if (validation == null)
            {
                continue;
            }

            if (formGroup.CheckBox?.CheckBoxes != null)
            {
                var parent = formGroup.CheckBox.CheckBoxes.FirstOrDefault(cb =>
                    (cb.ConditionalInputs?.Any(i => i.Name == component.Name) ?? false) ||
                    (cb.ConditionalSelects?.Any(s => s.Name == component.Name) ?? false));

                if (parent != null && !string.IsNullOrWhiteSpace(parent.Value))
                {
                    var parentValue = parent.Value.Trim().ToLowerInvariant();
                    if (!selectedCheckboxValues.Contains(parentValue))
                    {
                        continue;
                    }
                }
            }

            if (!answerValue.TryGetValue(component.Name, out var answerElement))
            {
                if (validation.Required == true)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = component is CheckBox
                            ? $"Select at least one option for {StringHelper.CapitaliseFirstLetter(component.Label)}"
                            : $"Enter {StringHelper.CapitaliseFirstLetter(component.Label)}"
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
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x))
                    .ToList()!;
                answerString = string.Join(", ", answerArray);
            }

            if (string.IsNullOrWhiteSpace(answerString) && (answerArray == null || !answerArray.Any()))
            {
                if (validation.Required == true)
                {
                    var label = StringHelper.CapitaliseFirstLetter(component.Label);
                    string message;

                    if (component is CheckBox)
                    {
                        message = $"Select at least one option for {label}";
                    }
                    else if (component is RadioButton)
                    {
                        message = $"Select an option for {label}";
                    }
                    else
                    {
                        message = $"Enter {label}";
                    }

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
                        ErrorMessage = $"The {StringHelper.CapitaliseFirstLetter(component.Label)} \"{answerString}\" already exists in our records"
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

                    bool tooShort = validation.MinLength.HasValue && length < validation.MinLength.Value;
                    bool tooLong = validation.MaxLength.HasValue && length > validation.MaxLength.Value;

                    if (tooShort || tooLong)
                    {
                        string unit = "words";
                        string message;

                        if (tooShort && (!validation.MaxLength.HasValue || validation.MaxLength == 0))
                        {
                            message = $"{StringHelper.CapitaliseFirstLetter(component.Label)} must be {validation.MinLength!.Value} {unit} or more";
                        }
                        else if (tooLong && (!validation.MinLength.HasValue || validation.MinLength == 0))
                        {
                            message = $"{StringHelper.CapitaliseFirstLetter(component.Label)} must be {validation.MaxLength!.Value} {unit} or fewer";
                        }
                        else
                        {
                            message = $"{StringHelper.CapitaliseFirstLetter(component.Label)} must be between {validation.MinLength!.Value} and {validation.MaxLength!.Value} {unit}";
                        }

                        errors.Add(new ValidationErrorItem
                        {
                            PropertyName = component.Name,
                            ErrorMessage = message
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
                        ErrorMessage = $"Enter a valid {StringHelper.CapitaliseFirstLetter(component.Label)}"
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
                        ErrorMessage = $"Select at least {validation.MinSelected.Value} option{(validation.MinSelected.Value > 1 ? "s" : "")} for {StringHelper.CapitaliseFirstLetter(component.Label)}"
                    });
                    continue;
                }

                if (validation.MaxSelected.HasValue && selectedCount > validation.MaxSelected.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"You can only select up to {validation.MaxSelected.Value} option{(validation.MaxSelected.Value > 1 ? "s" : "")} for {StringHelper.CapitaliseFirstLetter(component.Label)}"
                    });
                    continue;
                }
            }
        }

        return new ValidationResponse { Errors = errors };
    }
}