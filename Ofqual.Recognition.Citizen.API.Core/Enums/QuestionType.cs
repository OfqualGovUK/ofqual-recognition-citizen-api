using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Enums;

public enum QuestionType
{
    TextArea = 1,
    CheckBoxGroup = 2,
    RadioButtonGroup = 3,
    TextInputGroup = 4,
    FileUpload = 5,
    Review = 6,
    PreEngagement = 7
}