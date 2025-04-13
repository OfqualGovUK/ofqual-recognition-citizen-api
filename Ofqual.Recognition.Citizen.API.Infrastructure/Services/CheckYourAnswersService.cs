using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Newtonsoft.Json.Linq;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class CheckYourAnswersService : ICheckYourAnswersService
{
    public List<QuestionAnswerReviewDto> GetQuestionAnswers(IEnumerable<TaskQuestionAnswerDto> questions)
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

        var questionAnswers = new List<QuestionAnswerReviewDto>();

        foreach (var question in questions)
        {
            answerLookup.TryGetValue(question.QuestionId, out var parsedAnswer);
            var questionFields = ExtractQuestionFields(question, parsedAnswer?.AnswerData);

            foreach (var field in questionFields)
            {
                var answerValue = GetFieldAnswerValue(field.Name, parsedAnswer?.AnswerData);

                questionAnswers.Add(new QuestionAnswerReviewDto
                {
                    QuestionText = field.QuestionText,
                    AnswerValue = answerValue,
                    QuestionUrl = parsedAnswer?.QuestionUrl ?? question.QuestionUrl
                });
            }
        }

        return questionAnswers;
    }

    private static string GetFieldAnswerValue(string fieldName, JObject? answerData)
    {
        if (answerData == null)
        {
            return "Not provided";
        }

        var token = answerData[fieldName] ?? FindNestedAnswer(answerData, fieldName);
        return token != null ? FormatAnswerValue(token) : "Not provided";
    }

    private static string FormatAnswerValue(JToken answer)
    {
        return answer.Type == JTokenType.Array
            ? string.Join(", ", answer.Values<string>())
            : answer.ToString();
    }

    public static List<QuestionFieldDto> ExtractQuestionFields(TaskQuestionAnswerDto question, JObject? answerData = null)
    {
        var fields = new List<QuestionFieldDto>();

        if (string.IsNullOrWhiteSpace(question.QuestionContent))
        {
            return fields;
        }

        var questionJson = JObject.Parse(question.QuestionContent);
        var formGroup = questionJson["formGroup"] as JObject;

        if (formGroup == null)
        {
            return fields;
        }

        foreach (var formFieldGroup in formGroup.Properties())
        {
            var groupValue = formFieldGroup.Value;
            switch (formFieldGroup.Name)
            {
                case "TextInputs":
                    foreach (var input in groupValue)
                    {
                        AddQuestionField(fields, input.Value<string>("name"), input.Value<string>("label"));
                    }
                    break;

                case "Textarea":
                    AddQuestionField(fields, groupValue.Value<string>("name"), groupValue["label"]?.Value<string>("text"));
                    break;

                case "radioButton":
                    AddQuestionField(fields, groupValue.Value<string>("name"), groupValue["heading"]?.Value<string>("text"));
                    break;

                case "checkbox":
                    var checkboxName = groupValue.Value<string>("name");
                    var checkboxHeading = groupValue["heading"]?.Value<string>("text");
                    AddQuestionField(fields, checkboxName, checkboxHeading);

                    var selectedCheckboxes = answerData?[checkboxName]?.Values<string>().ToHashSet() ?? new HashSet<string>();
                    foreach (var checkboxOption in groupValue["checkBoxes"])
                    {
                        var checkboxValue = checkboxOption.Value<string>("value");
                        if (selectedCheckboxes.Contains(checkboxValue))
                        {
                            var conditionalFields = checkboxOption["conditionalInputs"] ?? checkboxOption["conditionalSelects"];
                            if (conditionalFields != null)
                            {
                                foreach (var conditionalField in conditionalFields)
                                {
                                    AddQuestionField(fields, conditionalField.Value<string>("name"), conditionalField.Value<string>("label"));
                                }
                            }
                        }
                    }
                    break;
            }
        }
        return fields;
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

    private static void AddQuestionField(List<QuestionFieldDto> fields, string? fieldName, string? questionText)
    {
        if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(questionText))
        {
            fields.Add(new QuestionFieldDto
            {
                Name = fieldName,
                QuestionText = questionText
            });
        }
    }
}