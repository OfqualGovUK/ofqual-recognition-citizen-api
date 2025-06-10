using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.Tests.Integration.Helper;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class StageRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public StageRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetFirstQuestionByStage_Should_Return_First_Question_In_StageOrder()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task1 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, sectionId: section.SectionId, taskNameUrl: "test", orderNumber: 2);
        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, sectionId: section.SectionId, taskNameUrl: "test", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var q1 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task1.TaskId,
            questionType.QuestionTypeId,
            order: 1,
            url: "q-task1",
            content: "{\"title\":\"second question\"}");

        var q2 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task2.TaskId,
            questionType.QuestionTypeId,
            order: 1,
            url: "q-task2",
            content: "{\"title\":\"first question\"}");

        await StageTestDataBuilder.CreateStageTask(unitOfWork, stageId: 1, taskId: task1.TaskId, order: 2);
        await StageTestDataBuilder.CreateStageTask(unitOfWork, stageId: 1, taskId: task2.TaskId, order: 1);

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetFirstQuestionByStage(StageEnum.PreEngagement);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(q2.QuestionId, result.QuestionId);
        Assert.Equal(task2.TaskId, result.TaskId);
        Assert.Equal(task2.TaskNameUrl, result.CurrentTaskNameUrl);
        Assert.Equal(q2.QuestionNameUrl, result.CurrentQuestionNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetStageQuestionByTaskAndQuestionUrl_Should_Return_Question_With_Correct_Navigation_Urls()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, sectionId: section.SectionId, taskNameUrl: "task-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            order: 1,
            url: "question-1",
            content: "{\"label\":\"First\"}");

        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            order: 2,
            url: "question-2",
            content: "{\"label\":\"Middle\"}");

        var question3 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            order: 3,
            url: "question-3",
            content: "{\"label\":\"Last\"}");

        await StageTestDataBuilder.CreateStageTask(unitOfWork, stageId: 1, taskId: task.TaskId, order: 1);

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetStageQuestionByTaskAndQuestionUrl(StageEnum.PreEngagement, "task-url", "question-2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question2.QuestionId, result.QuestionId);
        Assert.Equal(task.TaskId, result.TaskId);
        Assert.Equal(task.TaskNameUrl, result.CurrentTaskNameUrl);
        Assert.Equal(question2.QuestionNameUrl, result.CurrentQuestionNameUrl);
        Assert.Equal("question-1", result.PreviousQuestionNameUrl);
        Assert.Equal("question-3", result.NextQuestionNameUrl);
        Assert.Equal("task-url", result.PreviousTaskNameUrl);
        Assert.Equal("task-url", result.NextTaskNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetStageStatus_Should_Return_Expected_StageStatus()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);

        var stage = StageEnum.PreEngagement;
        var now = DateTime.UtcNow;

        var stageStatus = new StageStatus
        {
            ApplicationId = application.ApplicationId,
            StageId = stage,
            StatusId = TaskStatusEnum.Completed,
            StageStartDate = now.AddDays(-1),
            StageCompletionDate = now,
            CreatedByUpn = "creator@ofqual.gov.uk",
            ModifiedByUpn = "modifier@ofqual.gov.uk",
            CreatedDate = now.AddDays(-1),
            ModifiedDate = now
        };

        await StageTestDataBuilder.CreateStageStatus(unitOfWork, stageStatus);

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetStageStatus(application.ApplicationId, stage);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stageStatus.ApplicationId, result.ApplicationId);
        Assert.Equal(stageStatus.StageId, result.StageId);
        Assert.Equal(stageStatus.StatusId, result.StatusId);
        TestAssertHelpers.AssertDateTimeAlmostEqual(stageStatus.StageStartDate, result.StageStartDate);
        TestAssertHelpers.AssertDateTimeAlmostEqual(stageStatus.StageCompletionDate!.Value, result.StageCompletionDate!.Value);
        Assert.Equal(stageStatus.CreatedByUpn, result.CreatedByUpn);
        Assert.Equal(stageStatus.ModifiedByUpn, result.ModifiedByUpn);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}