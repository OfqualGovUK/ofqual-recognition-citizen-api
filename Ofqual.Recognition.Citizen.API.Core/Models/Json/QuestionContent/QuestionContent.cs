using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent
{
    /// <summary>
    /// Representation JSON Schema used for QuestionContent 
    /// </summary>
    public class QuestionContent
    {
        public string? Heading { get; set; }

        public string? Body { get; set; }
        public FormGroup? FormGroup { get; set; }
    }
}
