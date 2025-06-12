using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Newtonsoft.Json.Linq;
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

    public List<QuestionAnswerSectionDto> GetQuestionAnswers(IEnumerable<TaskQuestionAnswer> questions)
    {
        var answerLookup = questions
            .Where(q => !string.IsNullOrWhiteSpace(q.Answer))
            .ToDictionary(
                q => q.QuestionId,
                q => new ParsedQuestionAnswer
                {
                    AnswerData = JObject.Parse(q.Answer!),
                    QuestionUrl = $"{q.TaskNameUrl}/{q.QuestionNameUrl}"
                });

        var sections = new List<QuestionAnswerSectionDto>();

        foreach (var question in questions)
        {
            answerLookup.TryGetValue(question.QuestionId, out var parsedAnswer);

            if (string.IsNullOrWhiteSpace(question.QuestionContent))
            {
                continue;
            }

            var questionJson = JObject.Parse(question.QuestionContent);
            var formGroup = JsonHelper.GetObject(questionJson, "formGroup");

            if (formGroup == null)
            {
                continue;
            }

            foreach (var groupProperty in formGroup.Properties())
            {
                var groupValue = groupProperty.Value;
                var sectionHeading = JsonHelper.GetString(groupValue, "sectionName");

                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = sectionHeading
                };

                var textInputs = JsonHelper.GetArray(groupValue, "textInputs");
                if (textInputs != null)
                {
                    foreach (var input in textInputs)
                    {
                        var fieldName = JsonHelper.GetString(input, "name");
                        var label = JsonHelper.GetString(input, "label");

                        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(label))
                        {
                            continue;
                        }

                        var answerValue = JsonHelper.GetFlattenedStringValuesByKey(parsedAnswer?.AnswerData, fieldName);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                        });
                    }
                }

                if (groupProperty.Name.Equals("textarea", StringComparison.OrdinalIgnoreCase))
                {
                    var fieldName = JsonHelper.GetString(groupValue, "name");
                    var label = JsonHelper.GetNestedString(groupValue, "label", "text");

                    if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                    {
                        var answerValue = JsonHelper.GetFlattenedStringValuesByKey(parsedAnswer?.AnswerData, fieldName);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                        });
                    }
                }

                if (groupProperty.Name.Equals("radioButton", StringComparison.OrdinalIgnoreCase))
                {
                    var fieldName = JsonHelper.GetString(groupValue, "name");
                    var label = JsonHelper.GetNestedString(groupValue, "heading", "text");

                    if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                    {
                        var answerValue = JsonHelper.GetFlattenedStringValuesByKey(parsedAnswer?.AnswerData, fieldName);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                        });
                    }
                }

                if (groupProperty.Name.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    var checkboxName = JsonHelper.GetString(groupValue, "name");
                    var checkboxHeading = JsonHelper.GetNestedString(groupValue, "heading", "text");

                    if (!string.IsNullOrWhiteSpace(checkboxName) && !string.IsNullOrWhiteSpace(checkboxHeading))
                    {
                        var answerValue = JsonHelper.GetFlattenedStringValuesByKey(parsedAnswer?.AnswerData, checkboxName);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = checkboxHeading,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(checkboxName))
                    {
                        var selectedValues = JsonHelper.GetStringSetFromToken(parsedAnswer?.AnswerData?[checkboxName]);

                        foreach (var checkboxOption in JsonHelper.GetArray(groupValue, "checkBoxes") ?? Enumerable.Empty<JToken>())
                        {
                            var checkboxValue = JsonHelper.GetString(checkboxOption, "value");

                            if (!string.IsNullOrWhiteSpace(checkboxValue) && selectedValues.Contains(checkboxValue))
                            {
                                var conditionalFields = JsonHelper.GetArray(checkboxOption, "conditionalInputs")
                                    ?? JsonHelper.GetArray(checkboxOption, "conditionalSelects");

                                if (conditionalFields != null)
                                {
                                    foreach (var conditionalField in conditionalFields)
                                    {
                                        var fieldName = JsonHelper.GetString(conditionalField, "name");
                                        var label = JsonHelper.GetString(conditionalField, "label");

                                        if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                                        {
                                            var conditionalAnswer = JsonHelper.GetFlattenedStringValuesByKey(parsedAnswer?.AnswerData, fieldName);

                                            section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                                            {
                                                QuestionText = label,
                                                AnswerValue = conditionalAnswer,
                                                QuestionUrl = parsedAnswer?.QuestionUrl ?? $"{question.TaskNameUrl}/{question.QuestionNameUrl}"
                                            });
                                        }
                                    }
                                }
                            }
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