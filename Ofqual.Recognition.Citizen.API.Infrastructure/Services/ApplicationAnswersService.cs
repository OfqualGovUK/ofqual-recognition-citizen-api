using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Newtonsoft.Json.Linq;
using Ofqual.Recognition.Citizen.API.Core.Models.Applications;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;
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
            bool success = await _context.QuestionRepository.UpsertQuestionAnswer(applicationId, answer.QuestionId, answer.AnswerJson);
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

    public async Task<IEnumerable<ValidationErrorItemDto>?> ValidateQuestionAnswers(Guid taskId, Guid questionId, QuestionAnswerSubmissionDto answerDto)
    {

        var questionDetails = await _context.QuestionRepository.GetQuestion(taskId, questionId);
        if (questionDetails == null)
            return null;

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent);
                
        if (questionContent == null || questionContent.formGroup.Any())
            return null;


        List<IFormComponent> components = new List<IFormComponent>();
        var textItems = questionContent.formGroup["TextItems"];
        if (textItems != null)
        {
            components.AddRange((textItems as TextItems));
        }
        else
        {
            components.Add(questionContent.formGroup.First().Value);
        }



        var answerValue = JsonSerializer.Deserialize<Dictionary<string, string>>(answerDto.Answer);
        if (answerValue == null)
            return null;


        var errors = new List<ValidationErrorItemDto>();
        foreach (var answerItem in answerValue)
        {
            var component = components.First(x => x.Name != null
                         && x.Name.Equals(answerItem.Key,
                                          StringComparison.CurrentCultureIgnoreCase));


            if (component?.Validation == null)
                continue;

            if (component.Validation.Required ?? false)
            {
                if (string.IsNullOrWhiteSpace(answerItem.Value))
                    errors.Add(new ValidationErrorItemDto
                    {
                        Property = answerItem.Key,
                        ErrorMessage = $"Enter {answerItem.Key}"
                    });
                continue;
            }


            if (component.Validation.Unique ?? false)
            {
                if (await _context.QuestionRepository.CheckIfQuestionAnswerExists(taskId, questionId, answerItem.Key, answerItem.Value))
                    errors.Add(new ValidationErrorItemDto
                    {
                        Property = answerItem.Key,
                        ErrorMessage = $"The {answerItem.Key} \"{answerItem.Value}\" already exists within our records"
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
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = $"answer does not match the required format" });
                    continue;
                }
            }

            if (component is RadioButton button)
            {
                if (component.Validation.MinSelected.HasValue)
                {
                    if (button.Radios.Count() < component.Validation.MinSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "minimum number of items has not been selected" });
                    continue;
                }
                if (component.Validation.MaxSelected.HasValue)
                {
                    if (button.Radios.Count() < component.Validation.MaxSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "too many items have been selected" });
                    continue;
                }
            }
        }
        return errors;
    }

    private static ValidationErrorItemDto? ValidateTextLength(IFormComponent component, KeyValuePair<string, string> answerItem)
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
                Property = answerItem.Key,
                ErrorMessage = $"{answerItem.Key} must be "
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