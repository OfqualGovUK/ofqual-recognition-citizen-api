using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Models.QuestionContent;

public class FormGroup 
{ 
    List<FormGroupComponent> components { set; get; } = new List<FormGroupComponent>();
}

public abstract class FormGroupComponent 
{ 
    public string? heading {  get; set; }
    public string? hint { get; set; }
    public string? name { get; set; }

    public ComponentValidation? validation { get; set; }

}

public class ComponentItem
{
    public string label { get; set; } = string.Empty;
    public string value { get; set; } = string.Empty;
}

public class RadioButton : FormGroupComponent
{
    IEnumerable<RadioButton> radios { get; set; } = new List<RadioButton>();
}

public class TextInputItem
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

    public ComponentValidation? Validation { get; set; }
}

public class CheckBoxItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool? Selected { get; set; } = null;
}


public class TextInput: FormGroupComponent
{
    public IEnumerable<TextInputItem> TextInputs { get; set; } = new List<TextInputItem>();
}


public class CheckBoxConditionalInputItem
{     
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

}

public class CheckBoxConditionalSelectItem
{
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

    public IEnumerable<CheckBoxItem> ConditionalSelects { get; set; } = new List<CheckBoxItem>();
}




public class CheckBox : FormGroupComponent
{
    IEnumerable<CheckBoxItem> CheckBoxes { get; set; } = new List<CheckBoxItem>();
}