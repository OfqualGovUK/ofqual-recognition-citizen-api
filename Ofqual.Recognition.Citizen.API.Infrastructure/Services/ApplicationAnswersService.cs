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

    public async Task<ValidationResponse> ValidateQuestionAnswers(Guid taskId, Guid questionId, QuestionAnswerSubmissionDto answerDto)
    {

        var questionDetails = await _context.QuestionRepository.GetQuestion(taskId, questionId);
        if (questionDetails == null)
        {
            return new ValidationResponse
            {
                Message = "Unable to validate user response",
            };
        }

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


        var formGroup = questionContent?.FormGroup;
        if (formGroup == null)
        {
            return new ValidationResponse
            {
                Message = "Unable to validate user response",
            };
        }

        List<IValidatable> components = new List<IValidatable>();

        if(formGroup.Textarea != null) 
            components.Add(formGroup.Textarea);

        if(formGroup.RadioButton != null)
            components.Add(formGroup.RadioButton);

        if (formGroup.TextInput != null)
            components.AddRange(formGroup.TextInput.TextInputs);

        if (formGroup.CheckBox != null)
        {
            //add checkbox validation rule 
            components.Add(formGroup.CheckBox);
            
            //add any child combobox conditional select item validation rules
            var checkboxes = formGroup.CheckBox.CheckBoxes.SelectMany(x => x.ConditionalSelects ?? new List<Select>());
            if (checkboxes != null)
                components.AddRange(checkboxes);

            //add any child combobox conditional text field validation rules
            var textInputs = formGroup.CheckBox.CheckBoxes.SelectMany(x => x.ConditionalInputs ?? new List<TextInputItem>());
            if (textInputs != null)
                components.AddRange(textInputs);
        }

        var answerValue = JsonSerializer.Deserialize<Dictionary<string, string>>(answerDto.Answer, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (answerValue == null)
        {
            return new ValidationResponse
            {
                Message = "Unable to validate user response",
            };
        }

        var errors = new List<ValidationErrorItemDto>();
        foreach (var component in components)
        {
            if (component.Validation == null)
                continue;

            var answerItem = answerValue
                .DefaultIfEmpty(new KeyValuePair<string, string>(component.Name, string.Empty))
                .FirstOrDefault(x => x.Key.Equals(component.Name, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(answerItem.Value))                
            {
                if (component.Validation.Required ?? false)
                    errors.Add(new ValidationErrorItemDto
                    {
                        PropertyName = answerItem.Key,
                        ErrorMessage = $"Enter {component.Label}"
                    });
                continue;
            }

            if (component.Validation.Unique ?? false)
            {
                if (await _context.QuestionRepository.CheckIfQuestionAnswerExists(taskId, questionId, answerItem.Key, answerItem.Value))
                    errors.Add(new ValidationErrorItemDto
                    {
                        PropertyName = answerItem.Key,
                        ErrorMessage = $"The {component.Label} \"{answerItem.Value}\" already exists within our records"
                    });
                continue;
            }

            if (component is TextInput)
            {
                var textLengthError = ValidateTextLength(component, answerItem);
                if (textLengthError != null)
                {
                    errors.Add(textLengthError);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(component.Validation.Pattern))
                {
                    var regex = new Regex(component.Validation.Pattern);
                    if (!regex.IsMatch(answerItem.Value))
                        errors.Add(new ValidationErrorItemDto { PropertyName = answerItem.Key, ErrorMessage = $"{component.Label} does not match the required format" });
                    continue;
                }
            }

            if (component is RadioButton button)
            {
                if (component.Validation.MinSelected.HasValue)
                {
                    if (button.Radios.Count < component.Validation.MinSelected)
                        errors.Add(new ValidationErrorItemDto 
                        { 
                            PropertyName = answerItem.Key, 
                            ErrorMessage = $"minimum number of items has not been selected for {component.Label}" 
                        });
                    continue;
                }
                if (component.Validation.MaxSelected.HasValue)
                {
                    if (button.Radios.Count < component.Validation.MaxSelected)
                        errors.Add(new ValidationErrorItemDto 
                        { 
                            PropertyName = answerItem.Key, 
                            ErrorMessage = $"too many items have been selected for {component.Label}" 
                        });
                    continue;
                }
            }
        }
        return new ValidationResponse 
        {
            Errors = errors,
        };
    }

    private static ValidationErrorItemDto? ValidateTextLength(IValidatable component, KeyValuePair<string, string> answerItem)
    {
        var hasMinValue = component.Validation?.MinLength.HasValue ?? false;
        var hasMaxValue = component.Validation?.MaxLength.HasValue ?? false;

        //Skip if we dont have min or max values set
        if (!hasMinValue && !hasMaxValue)
            return null;

        //are we counting characters or words?
        var countWords = component.Validation?.CountWords ?? false;
        var answerLength = countWords
            ? answerItem.Value.Split(['\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).Length
            : answerItem.Value.Length;

        //WHERE minimum value is set AND size is smaller than minimum value
        //OR maximum value is set AND size is bigger than maxiimum value
        //THEN return an error
        if ((hasMinValue && answerLength < component.Validation!.MinLength)
        || (hasMaxValue && answerLength > component.Validation!.MaxLength))
        {
            var itemDto = new ValidationErrorItemDto
            {
                PropertyName = answerItem.Key,
                ErrorMessage = $"{component.Label} must be "
            };

            var countType = countWords ? "words" : "characters";

            if (!hasMinValue)
                itemDto.ErrorMessage += $"{component.Validation!.MaxLength} {countType} or more";
            else if (!hasMaxValue)
                itemDto.ErrorMessage += $"{component.Validation!.MinLength} {countType} or less";
            else
                itemDto.ErrorMessage += $"between {component.Validation!.MinLength} "
                    + $"and {component.Validation!.MaxLength} {countType}";
            return itemDto;
        }
        return null;
    }

}