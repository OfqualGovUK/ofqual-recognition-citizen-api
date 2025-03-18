using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IQuestionType
{
    public Guid QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; }
}

