using Newtonsoft.Json.Linq;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a parsed answer with its corresponding question URL.
/// </summary>
public class ParsedQuestionAnswer
{
    public JObject AnswerData { get; set; }
    public string QuestionUrl { get; set; }
}