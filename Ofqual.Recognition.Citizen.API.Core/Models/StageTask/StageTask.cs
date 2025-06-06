using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Models.StageTask
{
    public class StageTask
    {
        public int StageId { get; set; }
        public required string Stage { get; set; }
        public Guid TaskId { get; set; }
        public required string Task { get; set; }
        public int OrderNumber { get; set; }
    }
}
