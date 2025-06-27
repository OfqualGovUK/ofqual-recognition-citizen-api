using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Models.ApplicationAnswers
{
    public class ApplicationReviewSectionDto : ISection
    {
        public Guid SectionId { get; set; }
        public required string SectionName { get; set; }
        public int SectionOrderNumber { get; set; }

        public string Answer { get; set; }
    }
}
