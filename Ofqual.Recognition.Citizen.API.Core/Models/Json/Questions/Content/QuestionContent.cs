namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class QuestionContent
{
    public List<BodyItem>? Body { get; set; }
    public Sidebar? Sidebar { get; set; }
    public FormGroup? FormGroup { get; set; }
}