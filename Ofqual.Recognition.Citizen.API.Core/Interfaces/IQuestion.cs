using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IQuestion
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public int QuestionOrderNumber { get; set; }
    public Guid QuestionTypeId { get; set; }
    public string QuestionContent { get; set; }
    public string QuestionURL { get; set; }
}

