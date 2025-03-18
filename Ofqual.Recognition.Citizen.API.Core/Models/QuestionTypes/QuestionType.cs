namespace Ofqual.Recognition.Citizen.API.Core.Models.QuestionType;

public class QuestionType : IDataMetadata, IQuestionType
{
    public Guid QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}