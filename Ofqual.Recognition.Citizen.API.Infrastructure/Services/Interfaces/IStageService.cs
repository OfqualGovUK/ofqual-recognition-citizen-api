using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces
{
    public interface IStageService
    {
        /// <summary>
        /// Upsert Stage Statuses using applicationId and stage.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="stage"></param>
        /// <returns></returns>
        Task<bool> UpsertStageStatuses(Guid applicationId, StageEnum stage);
    }
}
