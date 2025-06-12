using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Newtonsoft.Json.Linq;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

using System.Text.RegularExpressions;
using System.Text.Json;
using Ofqual.Recognition.API.Models.JSON.Questions;

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

    public async Task<ValidationResponse> ValidateQuestionAnswers(Guid taskId, Guid questionId, string answerJson)
    {
        var questionDetails = await _context.QuestionRepository.GetQuestion(taskId, questionId);
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
        }

        var selectedCheckboxValues = new List<string>();
        if (formGroup.CheckBox?.Name != null &&
            answerValue.TryGetValue(formGroup.CheckBox.Name, out var checkboxAnswerElement) &&
            checkboxAnswerElement.ValueKind == JsonValueKind.Array)
        {
            selectedCheckboxValues = checkboxAnswerElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToList();
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
                bool isConditional = formGroup.CheckBox.CheckBoxes.Any(cb =>
                    (cb.ConditionalInputs?.Any(i => i.Name == component.Name) ?? false)
                    || (cb.ConditionalSelects?.Any(s => s.Name == component.Name) ?? false));

                if (isConditional)
                {
                    var parent = formGroup.CheckBox.CheckBoxes.FirstOrDefault(cb =>
                        (cb.ConditionalInputs?.Any(i => i.Name == component.Name) ?? false)
                        || (cb.ConditionalSelects?.Any(s => s.Name == component.Name) ?? false));

                    if (parent != null && parent.Value != null && !selectedCheckboxValues.Contains(parent.Value))
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
                            ? $"Select at least one option for {ToSentenceCase(component.Label)}"
                            : $"Enter {ToSentenceCase(component.Label)}"
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

            bool isEmpty = string.IsNullOrWhiteSpace(answerString) && (answerArray == null || !answerArray.Any());

            if (isEmpty)
            {
                if (validation.Required == true)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = component is CheckBox
                            ? $"Select at least one option for {ToSentenceCase(component.Label)}"
                            : $"Enter {ToSentenceCase(component.Label)}"
                    });
                }
                continue;
            }

            if (validation.Unique == true && !string.IsNullOrWhiteSpace(answerString))
            {
                if (await _context.QuestionRepository.CheckIfQuestionAnswerExists(taskId, questionId, component.Name, answerString))
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"The {ToSentenceCase(component.Label)} \"{answerString}\" already exists in our records"
                    });
                }
                continue;
            }

            var lengthError = ValidateTextLength(component.Name, ToSentenceCase(component.Label), answerString!, validation);
            if (lengthError != null)
            {
                errors.Add(lengthError);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(validation.Pattern))
            {
                var regex = new Regex(validation.Pattern);
                if (!regex.IsMatch(answerString ?? ""))
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"Enter a valid {ToSentenceCase(component.Label)}"
                    });
                    continue;
                }
            }

            if (component is CheckBox && answerArray != null)
            {
                int selectedCount = answerArray.Count;
                if (validation.MinSelected.HasValue && selectedCount < validation.MinSelected.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"Select at least {validation.MinSelected.Value} option{(validation.MinSelected.Value > 1 ? "s" : "")} for {ToSentenceCase(component.Label)}"
                    });
                    continue;
                }

                if (validation.MaxSelected.HasValue && selectedCount > validation.MaxSelected.Value)
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"You can only select up to {validation.MaxSelected.Value} option{(validation.MaxSelected.Value > 1 ? "s" : "")} for {ToSentenceCase(component.Label)}"
                    });
                    continue;
                }
            }

            if (component is Select selectComponent && answerString != null)
            {
                var validValues = selectComponent.Options?
                    .Where(o => !string.IsNullOrEmpty(o.Value))
                    .Select(o => o.Value)
                    .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

                if (validValues != null && !validValues.Contains(answerString))
                {
                    errors.Add(new ValidationErrorItem
                    {
                        PropertyName = component.Name,
                        ErrorMessage = $"Select a valid option for {ToSentenceCase(component.Label)}"
                    });
                    continue;
                }
            }
        }

        return new ValidationResponse { Errors = errors };
    }

    private static ValidationErrorItem? ValidateTextLength(string name, string label, string value, ValidationRule validation)
    {
        if (validation == null)
        {
            return null;
        }

        var min = validation.MinLength;
        var max = validation.MaxLength;

        if (!min.HasValue && !max.HasValue)
        {
            return null;
        }

        bool countWords = validation.CountWords == true;

        if (!countWords)
        {
            return null;
        }

        int length = countWords
            ? value.Split(['\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).Length
            : value.Length;
        
        bool tooShort = min.HasValue && length < min.Value;
        bool tooLong = max.HasValue && length > max.Value;


        if (!tooShort && !tooLong)
        {
            return null;
        }

        string unit = countWords ? "words" : "characters";
        string message;

        if (tooShort && (!max.HasValue || max == 0))
        {
            message = $"{label} must be {min!.Value} {unit} or more";
        }
        else if (tooLong && (!min.HasValue || min == 0))
        {
            message = $"{label} must be {max!.Value} {unit} or fewer";
        }
        else
        {
            message = $"{label} must be between {min!.Value} and {max!.Value} {unit}";
        }

        return new ValidationErrorItem
        {
            PropertyName = name,
            ErrorMessage = message
        };
    }

    private string ToSentenceCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1).ToLowerInvariant();
    }
}