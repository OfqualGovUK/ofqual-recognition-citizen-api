using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent
{
    /// <summary>
    /// Representation JSON Schema used for QuestionContent 
    /// </summary>
    public class QuestionContent
    {
        public string? heading { get; set; }

        public string? body { get; set; }
        public Dictionary<string, IFormComponent> formGroup { get; set; } = new Dictionary<string, IFormComponent>();
    }
}
