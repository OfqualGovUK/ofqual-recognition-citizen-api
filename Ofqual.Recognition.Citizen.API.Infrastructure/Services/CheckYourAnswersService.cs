using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Newtonsoft.Json.Linq;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class CheckYourAnswersService : ICheckYourAnswersService
{
    public List<QuestionAnswerSectionDto> GetQuestionAnswers(IEnumerable<TaskQuestionAnswerDto> questions)
    {
        var answerLookup = questions
            .Where(q => !string.IsNullOrWhiteSpace(q.Answer))
            .ToDictionary(
                q => q.QuestionId,
                q => new ParsedQuestionAnswerDto
                {
                    AnswerData = JObject.Parse(q.Answer),
                    QuestionUrl = q.QuestionUrl
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
            var formGroup = questionJson["formGroup"] as JObject;

            if (formGroup == null)
            {
                continue;
            }

            foreach (var groupProperty in formGroup.Properties())
            {
                var groupValue = groupProperty.Value;
                var sectionHeading = groupValue.Value<string>("SectionName");

                var section = new QuestionAnswerSectionDto
                {
                    SectionHeading = sectionHeading
                };

                var textInputs = groupValue["TextInputs"] as JArray;
                if (textInputs != null)
                {
                    foreach (var input in textInputs)
                    {
                        var fieldName = input.Value<string>("name");
                        var label = input.Value<string>("label");

                        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(label))
                        {
                            continue;
                        }

                        var answerValue = GetFieldAnswerValue(fieldName, parsedAnswer?.AnswerData);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                        });
                    }
                }
                if (groupProperty.Name == "Textarea")
                {
                    var fieldName = groupValue.Value<string>("name");
                    var label = groupValue["label"]?.Value<string>("text");

                    if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                    {
                        var answerValue = GetFieldAnswerValue(fieldName, parsedAnswer?.AnswerData);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                        });
                    }
                }
                if (groupProperty.Name == "radioButton")
                {
                    var fieldName = groupValue.Value<string>("name");
                    var label = groupValue["heading"]?.Value<string>("text");

                    if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                    {
                        var answerValue = GetFieldAnswerValue(fieldName, parsedAnswer?.AnswerData);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = label,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                        });
                    }
                }
                if (groupProperty.Name == "checkbox")
                {
                    var checkboxName = groupValue.Value<string>("name");
                    var checkboxHeading = groupValue["heading"]?.Value<string>("text");

                    if (!string.IsNullOrWhiteSpace(checkboxName) && !string.IsNullOrWhiteSpace(checkboxHeading))
                    {
                        var answerValue = GetFieldAnswerValue(checkboxName, parsedAnswer?.AnswerData);

                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                        {
                            QuestionText = checkboxHeading,
                            AnswerValue = answerValue,
                            QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                        });
                    }

                    var selectedCheckboxes = GetCheckboxValues(parsedAnswer?.AnswerData?[checkboxName]);
                    foreach (var checkboxOption in groupValue["checkBoxes"] ?? Enumerable.Empty<JToken>())
                    {
                        var checkboxValue = checkboxOption.Value<string>("value");
                        if (selectedCheckboxes.Contains(checkboxValue))
                        {
                            var conditionalFields = checkboxOption["conditionalInputs"] ?? checkboxOption["conditionalSelects"];
                            if (conditionalFields != null)
                            {
                                foreach (var conditionalField in conditionalFields)
                                {
                                    var fieldName = conditionalField.Value<string>("name");
                                    var label = conditionalField.Value<string>("label");

                                    if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(label))
                                    {
                                        var conditionalAnswerValue = GetFieldAnswerValue(fieldName, parsedAnswer?.AnswerData);

                                        section.QuestionAnswers.Add(new QuestionAnswerReviewDto
                                        {
                                            QuestionText = label,
                                            AnswerValue = conditionalAnswerValue,
                                            QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                                        });
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

    private static List<string> GetFieldAnswerValue(string fieldName, JObject? answerData)
    {
        if (answerData == null)
        {
            return new List<string> { "Not provided" };
        }

        var token = answerData[fieldName] ?? FindNestedAnswer(answerData, fieldName);

        if (token == null)
        {
            return new List<string> { "Not provided" };
        }

        return token.Type == JTokenType.Array
            ? token.Values<string>().ToList()
            : new List<string> { token.ToString() };
    }

    private static JToken? FindNestedAnswer(JToken? token, string fieldName)
    {
        if (token == null)
        {
            return null;
        }

        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                if (property.Name == fieldName)
                {
                    return property.Value;
                }
                
                var nestedResult = FindNestedAnswer(property.Value, fieldName);
                if (nestedResult != null)
                {
                    return nestedResult;
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token)
            {
                var nestedResult = FindNestedAnswer(item, fieldName);
                if (nestedResult != null)
                {
                    return nestedResult;
                }
            }
        }
        return null;
    }

    private static HashSet<string> GetCheckboxValues(JToken? token)
    {
        return token switch
        {
            JArray array => array.Values<string>().ToHashSet(),
            JValue value when !string.IsNullOrWhiteSpace(value.ToString()) => new HashSet<string> { value.ToString()! },
            _ => new HashSet<string>()
        };
    }
}
