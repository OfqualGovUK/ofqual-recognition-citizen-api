using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents an answer submitted for an application question.
/// </summary>
public class ApplicationAnswerDto : IApplicationAnswer
{
    public string Answer { get; set; }
}