using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class QuestionRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public QuestionRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuestionByQuestionId_Should_Return_Correct_Question_With_Navigation_Urls()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-url",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var q1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"First\"}",
            QuestionNameUrl = "question-1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var q2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 2,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"Middle\"}",
            QuestionNameUrl = "question-2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var q3 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 3,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"Last\"}",
            QuestionNameUrl = "question-3",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestionByQuestionId(q2.QuestionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(q2.QuestionId, result!.QuestionId);
        Assert.Equal(q2.QuestionContent, result.QuestionContent);
        Assert.Equal(q2.TaskId, result.TaskId);
        Assert.Equal(q2.QuestionNameUrl, result.CurrentQuestionNameUrl);
        Assert.Equal(q1.QuestionNameUrl, result.PreviousQuestionNameUrl);
        Assert.Equal(q3.QuestionNameUrl, result.NextQuestionNameUrl);
        Assert.Equal(task.TaskNameUrl, result.TaskNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllQuestions_Should_Return_All_Inserted_Questions()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-name-url",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"One\"}",
            QuestionNameUrl = "question-1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 2,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Two\"}",
            QuestionNameUrl = "question-2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.QuestionRepository.GetAllQuestions()).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, q => q.QuestionId == question1.QuestionId);
        Assert.Contains(result, q => q.QuestionId == question2.QuestionId);

        Assert.All(result.Where(q => q.QuestionId == question1.QuestionId || q.QuestionId == question2.QuestionId), q =>
        {
            Assert.NotEqual(Guid.Empty, q.QuestionId);
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionContent));
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionNameUrl));
            Assert.NotEqual(default, q.CreatedDate);
        });

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuestionByTaskAndQuestionUrl_Should_Return_Correct_Navigation_Details()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "test-task-url",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"First\"}",
            QuestionNameUrl = "question-1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 2,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Middle\"}",
            QuestionNameUrl = "question-2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question3 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 3,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Last\"}",
            QuestionNameUrl = "question-3",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestionByTaskAndQuestionUrl(task.TaskNameUrl, question2.QuestionNameUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question2.QuestionId, result!.QuestionId);
        Assert.Equal(task.TaskId, result.TaskId);
        Assert.Equal(task.TaskNameUrl, result.TaskNameUrl);
        Assert.Equal(question2.QuestionNameUrl, result.CurrentQuestionNameUrl);
        Assert.Equal(question1.QuestionNameUrl, result.PreviousQuestionNameUrl);
        Assert.Equal(question3.QuestionNameUrl, result.NextQuestionNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}