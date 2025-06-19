namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class FormGroup
{
    public Textarea? Textarea { get; set; }
    public RadioButtonGroup? RadioButtonGroup { get; set; }
    public CheckBoxGroup? CheckboxGroup { get; set; }
    public TextInputGroup? TextInputGroup { get; set; }
    public FileUpload? FileUpload { get; set; }
}