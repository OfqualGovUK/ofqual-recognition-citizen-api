namespace Ofqual.Recognition.API.Models.JSON.Questions;
public class QuestionContent
{
    public string? Heading { get; set; }
    public List<BodyItem>? Body { get; set; }
    public List<HelpItem>? Help { get; set; }
    public FormGroup? FormGroup { get; set; }
}