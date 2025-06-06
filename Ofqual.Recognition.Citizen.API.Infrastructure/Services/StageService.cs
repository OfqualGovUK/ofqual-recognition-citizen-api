using Castle.DynamicProxy;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.StageStatus;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services
{
    public class StageService : IStageService
    {
        private readonly IUnitOfWork _context;

        public StageService(IUnitOfWork context)
        {
            _context = context;
        }

        public async Task<bool> UpsertStageStatuses(Guid applicationId, StageEnum stage)
        {
            // Get all tasks by stage
            var stageTasks = await _context.StageRepository.GetAllStageTasksByStage(stage);

            if (stageTasks == null || !stageTasks.Any())
            {
                return false;
            }

            //Get the pre engagement answers
            var answers = await _context.ApplicationRepository.GetAllApplicationAnswers(applicationId);

            if (answers == null || !answers.Any())
            {
                return false;
            }

            var questions = await _context.QuestionRepository.GetAllQuestions();

            if (questions == null || !questions.Any())
            {
                return false;
            }

            // Checks all of the taskIds from stage task table. Check if all of the task ids match with the question table.
            // Within the questions, if all of the questionIds match within the answers table.
            // If a question id within an answers table doesn't match the question id there it fails.

            var allTasksCompleted = stageTasks.Select(t => t.TaskId).All(taskId => {
                return questions.Where(q => q.TaskId == taskId).All(q => {
                    return answers.Any(a => {
                        return a.QuestionId == q.QuestionId && a.ApplicationId == applicationId;
                        });
                });
            } );

            StageStatus stageStatus = new StageStatus()
            {
                StageId = stage,
                StatusId = TaskStatusEnum.Completed,
                StageStartDate = DateTime.UtcNow,
                StageCompletionDate = DateTime.UtcNow,
                CreatedByUpn = "USER",
                ModifiedByUpn = "USER RICH NEW"
            };

            if (allTasksCompleted)
            {
                await _context.StageRepository.UpsertStageStatusRecord(applicationId, stageStatus);
            }          

            return true;
        }
    }
}
