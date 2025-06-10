namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class FormGroup 
{
    public Textarea? Textarea { get; set; }
    public RadioButton? RadioButton { get; set; }
    public CheckBox? CheckBox { get; set; }
    public TextInput? TextInput { get; set; }
}