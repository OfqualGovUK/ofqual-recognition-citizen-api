using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class TaskTestDataBuilder
{
    public static async Task<TaskItem> CreateTestTask(UnitOfWork unitOfWork, TaskItem task)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Task]
            (TaskId, TaskName, TaskNameUrl, OrderNumber, ReviewFlag, HintText, SectionId, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@TaskId, @TaskName, @TaskNameUrl, @TaskOrderNumber, @ReviewFlag, @HintText, @SectionId, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            task,
            unitOfWork.Transaction);

        return task;
    }

    public static async Task<Section> CreateTestSection(UnitOfWork unitOfWork, Section section)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Section]
            (SectionId, SectionName, OrderNumber, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@SectionId, @SectionName, @SectionOrderNumber, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            section,
            unitOfWork.Transaction);
        return section;
    }

    public static async Task<TaskItemStatus> CreateTestTaskStatus(UnitOfWork unitOfWork, TaskItemStatus taskStatus)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[TaskStatus]
            (TaskStatusId, ApplicationId, TaskId, Status, CreatedByUpn, CreatedDate, ModifiedDate)
            VALUES (@TaskStatusId, @ApplicationId, @TaskId, @Status, @CreatedByUpn, @CreatedDate, @ModifiedDate);",
            new
            {
                taskStatus.TaskStatusId,
                taskStatus.ApplicationId,
                taskStatus.TaskId,
                Status = (int)taskStatus.Status,
                taskStatus.CreatedByUpn,
                taskStatus.CreatedDate,
                taskStatus.ModifiedDate
            },
            unitOfWork.Transaction);

        return taskStatus;
    }
}