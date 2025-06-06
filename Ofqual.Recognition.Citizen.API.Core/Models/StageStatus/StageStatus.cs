using Ofqual.Recognition.Citizen.API.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Models.StageStatus
{
    public class StageStatus
    {
        public Guid StageStatusId { get; set; }
        public StageEnum StageId { get; set; }
        public TaskStatusEnum StatusId { get; set; }
        public DateTime StageStartDate { get; set; }
        public DateTime? StageCompletionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public required string CreatedByUpn { get; set; }
        public string? ModifiedByUpn { get; set; }
    }
}
