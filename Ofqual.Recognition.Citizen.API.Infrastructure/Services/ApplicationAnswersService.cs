using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Helpers;
using Newtonsoft.Json.Linq;

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
}