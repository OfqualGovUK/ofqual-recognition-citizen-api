using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Helper;

public static class TaskTestDataBuilder
{
    public static async Task<TaskItem> CreateTestTask(UnitOfWork unitOfWork, Guid sectionId)
    {
        var task = new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskOrderNumber = 1,
            SectionId = sectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Task]
            (TaskId, TaskName, OrderNumber, SectionId, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@TaskId, @TaskName, @TaskOrderNumber, @SectionId, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            task,
            unitOfWork.Transaction);

        return task;
    }

    public static async Task<Section> CreateTestSection(UnitOfWork unitOfWork)
    {
        var section = new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Section]
            (SectionId, SectionName, OrderNumber, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@SectionId, @SectionName, @SectionOrderNumber, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            section,
            unitOfWork.Transaction);

        return section;
    }

    public static async Task<TaskItemStatus> CreateTestTaskStatus(UnitOfWork unitOfWork, Guid applicationId, TaskItem task)
    {
        var status = new TaskItemStatus
        {
            TaskStatusId = Guid.NewGuid(),
            ApplicationId = applicationId,
            TaskId = task.TaskId,
            Status = TaskStatusEnum.NotStarted,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[TaskStatus]
            (TaskStatusId, ApplicationId, TaskId, Status, CreatedByUpn, CreatedDate, ModifiedDate)
            VALUES (@TaskStatusId, @ApplicationId, @TaskId, @Status, @CreatedByUpn, @CreatedDate, @ModifiedDate);",
            new
            {
                status.TaskStatusId,
                status.ApplicationId,
                status.TaskId,
                Status = (int)status.Status,
                status.CreatedByUpn,
                status.CreatedDate,
                status.ModifiedDate
            },
            unitOfWork.Transaction);
        
        return status;
    }
}