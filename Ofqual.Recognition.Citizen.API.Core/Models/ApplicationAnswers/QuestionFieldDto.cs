namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a field extracted from a question's structure.
/// </summary>
public class QuestionFieldDto
{
    public string Name { get; set; }
    public string QuestionText { get; set; }
}