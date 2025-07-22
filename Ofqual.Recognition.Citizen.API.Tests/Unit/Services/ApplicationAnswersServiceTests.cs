using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Text.Json;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class ApplicationAnswersServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IQuestionRepository> _mockQuestionRepository = new();
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository = new();
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepository = new();
    private readonly Mock<ITaskStatusRepository> _mockTaskStatusRepository = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly Mock<IStageService> _mockStageService = new();
    private readonly ApplicationAnswersService _applicationAnswersService;

    public ApplicationAnswersServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);
        _mockUnitOfWork.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepository.Object);
        _mockUnitOfWork.Setup(u => u.TaskStatusRepository).Returns(_mockTaskStatusRepository.Object);

        _applicationAnswersService = new ApplicationAnswersService(
            _mockUnitOfWork.Object,
            _mockUserInformationService.Object,
            _mockStageService.Object
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitAnswerAndUpdateStatus_Should_Return_True_When_All_Operations_Succeed()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"value\":\"test answer\"}";
        var upn = "test@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(x => x.GetCurrentUserUpn())
            .Returns(upn);

        _mockApplicationAnswersRepository
            .Setup(x => x.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn))
            .ReturnsAsync(true);

        _mockTaskStatusRepository
            .Setup(x => x.UpdateTaskStatus(applicationId, taskId, StatusType.InProgress, upn))
            .ReturnsAsync(true);

        _mockStageService
            .Setup(x => x.EvaluateAndUpsertAllStageStatus(applicationId))
            .ReturnsAsync(true);

        // Act
        var result = await _applicationAnswersService.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, answerJson);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitAnswerAndUpdateStatus_Should_Return_False_When_Upserting_Answer_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"value\":\"invalid answer\"}";
        var upn = "test@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(x => x.GetCurrentUserUpn())
            .Returns(upn);

        _mockApplicationAnswersRepository
            .Setup(x => x.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn))
            .ReturnsAsync(false);

        // Act
        var result = await _applicationAnswersService.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, answerJson);

        // Assert
        Assert.False(result);

        // Verify no further calls
        _mockTaskStatusRepository.Verify(x => x.UpdateTaskStatus(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StatusType>(), It.IsAny<string>()), Times.Never);
        _mockStageService.Verify(x => x.EvaluateAndUpsertAllStageStatus(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitAnswerAndUpdateStatus_Should_Return_False_When_Updating_Task_Status_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"value\":\"test answer\"}";
        var upn = "test@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(x => x.GetCurrentUserUpn())
            .Returns(upn);

        _mockApplicationAnswersRepository
            .Setup(x => x.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn))
            .ReturnsAsync(true);

        _mockTaskStatusRepository
            .Setup(x => x.UpdateTaskStatus(applicationId, taskId, StatusType.InProgress, upn))
            .ReturnsAsync(false);

        // Act
        var result = await _applicationAnswersService.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, answerJson);

        // Assert
        Assert.False(result);

        // Verify no call to stage service
        _mockStageService.Verify(x => x.EvaluateAndUpsertAllStageStatus(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitAnswerAndUpdateStatus_Should_Return_False_When_Updating_Stage_Status_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"value\":\"test answer\"}";
        var upn = "test@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(x => x.GetCurrentUserUpn())
            .Returns(upn);

        _mockApplicationAnswersRepository
            .Setup(x => x.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn))
            .ReturnsAsync(true);

        _mockTaskStatusRepository
            .Setup(x => x.UpdateTaskStatus(applicationId, taskId, StatusType.InProgress, upn))
            .ReturnsAsync(true);

        _mockStageService
            .Setup(x => x.EvaluateAndUpsertAllStageStatus(applicationId))
            .ReturnsAsync(false);

        // Act
        var result = await _applicationAnswersService.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, answerJson);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SavePreEngagementAnswers_ShouldReturnTrue_WhenAllAnswersAreValidAndSaved()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"field\":\"value\"}";
        var upn = "test@ofqual.gov.uk";

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = questionId, AnswerJson = answerJson }
        };

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionTypeName = "TextInputGroup",
            CurrentQuestionNameUrl = "some-url",
            TaskNameUrl = "task-url",
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                    {
                        new()
                        {
                            Name = "field",
                            Label = "Field",
                            Validation = new ValidationRule { Required = true }
                        }
                    }
                    }
                }
            })
        };

        _mockUserInformationService.Setup(u => u.GetCurrentUserUpn()).Returns(upn);
        _mockQuestionRepository.Setup(r => r.GetQuestionByQuestionId(questionId)).ReturnsAsync(questionDetails);
        _mockApplicationAnswersRepository.Setup(r => r.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn)).ReturnsAsync(true);

        // Act
        var result = await _applicationAnswersService.SavePreEngagementAnswers(applicationId, preEngagementAnswers);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SavePreEngagementAnswers_ShouldReturnFalse_WhenValidationFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{}";

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = questionId, AnswerJson = answerJson }
        };

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionTypeName = "TextInputGroup",
            CurrentQuestionNameUrl = "url",
            TaskNameUrl = "task-url",
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                    {
                        new()
                        {
                            Name = "field",
                            Label = "Field",
                            Validation = new ValidationRule { Required = true }
                        }
                    }
                    }
                }
            })
        };

        _mockQuestionRepository.Setup(r => r.GetQuestionByQuestionId(questionId)).ReturnsAsync(questionDetails);

        // Act
        var result = await _applicationAnswersService.SavePreEngagementAnswers(applicationId, preEngagementAnswers);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SavePreEngagementAnswers_ShouldReturnFalse_WhenUpsertFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"field\":\"some value\"}";
        var upn = "test@ofqual.gov.uk";

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = questionId, AnswerJson = answerJson }
        };

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionTypeName = "TextInputGroup",
            CurrentQuestionNameUrl = "some-question",
            TaskNameUrl = "some-task",
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                    {
                        new()
                        {
                            Name = "field",
                            Label = "Field",
                            Validation = new ValidationRule { Required = true }
                        }
                    }
                    }
                }
            })
        };

        _mockUserInformationService.Setup(u => u.GetCurrentUserUpn()).Returns(upn);
        _mockQuestionRepository.Setup(r => r.GetQuestionByQuestionId(questionId)).ReturnsAsync(questionDetails);
        _mockApplicationAnswersRepository.Setup(r => r.UpsertQuestionAnswer(applicationId, questionId, answerJson, upn)).ReturnsAsync(false);

        // Act
        var result = await _applicationAnswersService.SavePreEngagementAnswers(applicationId, preEngagementAnswers);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskAnswerReview_ShouldReturnCombinedSection_ForAllControlTypes()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        var attachments = new List<Attachment>
        {
            new Attachment { FileName = "Zeta.pdf", FileMIMEtype = "application/pdf", CreatedByUpn = "user1", ModifiedByUpn = "user1" },
            new Attachment { FileName = "Alpha.pdf", FileMIMEtype = "application/pdf", CreatedByUpn = "user2", ModifiedByUpn = "user2" }
        };

        var taskQuestionAnswers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Criteria A",
                SectionOrderNumber = 1,
                ApplicationId = applicationId,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrderNumber = 1,
                ReviewFlag = true,
                QuestionId = questionId,
                QuestionNameUrl = "your-background",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Personal Details",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "txtName", Label = "Your name" }
                            }
                        },
                        Textarea = new Textarea
                        {
                            SectionName = "Experience",
                            Name = "txtArea1",
                            Label = new TextWithSize { Text = "Tell us about your experience" }
                        },
                        RadioButtonGroup = new RadioButtonGroup
                        {
                            SectionName = "Role",
                            Name = "radio1",
                            Heading = new TextWithSize { Text = "Select your role" },
                            Options = new List<RadioButtonItem>
                            {
                                new RadioButtonItem { Label = "Teacher", Value = "Teacher" },
                                new RadioButtonItem { Label = "Examiner", Value = "Examiner" }
                            }
                        },
                        CheckboxGroup = new CheckBoxGroup
                        {
                            SectionName = "Involvement",
                            Name = "checks",
                            Heading = new TextWithSize { Text = "Select areas of involvement" },
                            Options = new List<CheckBoxItem>
                            {
                                new CheckBoxItem
                                {
                                    Label = "Moderation",
                                    Value = "moderation",
                                    ConditionalInputs = new List<TextInputItem>
                                    {
                                        new TextInputItem
                                        {
                                            Name = "modDetails",
                                            Label = "Details of moderation role"
                                        }
                                    }
                                },
                                new CheckBoxItem
                                {
                                    Label = "Assessment",
                                    Value = "assessment"
                                }
                            }
                        },
                        FileUpload = new FileUpload
                        {
                            SectionName = "Supporting Documents",
                            Label = new TextWithSize { Text = "Upload your documents" },
                            Name = "uploads"
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "txtName", "Ofqual User" },
                    { "txtArea1", "Experience with qualification reviews" },
                    { "radio1", "Examiner" },
                    { "checks", new[] { "moderation" } },
                    { "modDetails", "I lead standardisation" }
                })
            }
        };

        _mockApplicationAnswersRepository
            .Setup(x => x.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(taskQuestionAnswers);

        _mockAttachmentRepository
            .Setup(x => x.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(attachments);

        // Act
        var result = await _applicationAnswersService.GetTaskAnswerReview(applicationId, taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var section = result.First();
        Assert.NotEmpty(section.QuestionAnswers);
        Assert.Equal(6, section.QuestionAnswers.Count);

        var fileAnswer = section.QuestionAnswers.FirstOrDefault(a => a.QuestionText == "Files you uploaded");
        Assert.NotNull(fileAnswer);

        var fileNames = fileAnswer!.AnswerValue!;
        Assert.Equal(2, fileNames.Count);
        Assert.Equal("Alpha.pdf", fileNames[0]);
        Assert.Equal("Zeta.pdf", fileNames[1]);
    }

    [Theory]
    [InlineData(null)] // Question not found
    [InlineData("{}")] // Question found but formGroup is missing
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnNull_WhenQuestionIsInvalid(string? questionContentJson)
    {
        // Arrange
        var questionId = Guid.NewGuid();

        _mockQuestionRepository
            .Setup(q => q.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(questionContentJson == null
                ? null
                : new QuestionDetails
                {
                    QuestionId = questionId,
                    TaskId = Guid.NewGuid(),
                    QuestionContent = questionContentJson,
                    CurrentQuestionNameUrl = "test-question",
                    QuestionTypeName = "TextInput",
                    TaskNameUrl = "test-task"
                });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, "{}");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnError_WhenAnswerMissingRequiredField()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var questionContent = new QuestionContent
        {
            FormGroup = new FormGroup
            {
                TextInputGroup = new TextInputGroup
                {
                    Fields = new List<TextInputItem>
                    {
                        new TextInputItem
                        {
                            Name = "input1",
                            Label = "Your name",
                            Validation = new ValidationRule { Required = true }
                        }
                    }
                }
            }
        };

        _mockQuestionRepository
            .Setup(q => q.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                TaskId = Guid.NewGuid(),
                QuestionContent = JsonSerializer.Serialize(questionContent),
                CurrentQuestionNameUrl = "test-question",
                QuestionTypeName = "TextInputGroup",
                TaskNameUrl = "test-task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, "{}");

        // Assert
        Assert.NotNull(result?.Errors);
        var firstError = result.Errors.First();
        Assert.Equal("Enter Your name", firstError.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnNoErrors_WhenAnswerIsValid()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var questionContent = new QuestionContent
        {
            FormGroup = new FormGroup
            {
                TextInputGroup = new TextInputGroup
                {
                    Fields = new List<TextInputItem>
                    {
                        new TextInputItem
                        {
                            Name = "input1",
                            Label = "Your name",
                            Validation = new ValidationRule { Required = true }
                        }
                    }
                }
            }
        };

        var validAnswer = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "input1", "Ofqual User" }
        });

        _mockQuestionRepository
            .Setup(q => q.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                TaskId = Guid.NewGuid(),
                QuestionContent = JsonSerializer.Serialize(questionContent),
                CurrentQuestionNameUrl = "test-question",
                QuestionTypeName = "TextInputGroup",
                TaskNameUrl = "test-task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, validAnswer);

        // Assert
        Assert.Empty(result?.Errors!);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnError_WhenAnswerIsNotUnique()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var questionContent = JsonSerializer.Serialize(new QuestionContent
        {
            FormGroup = new FormGroup
            {
                TextInputGroup = new TextInputGroup
                {
                    SectionName = "Details",
                    Fields = new List<TextInputItem>
                    {
                        new()
                        {
                            Name = "field",
                            Label = "Your name",
                            Validation = new ValidationRule { Unique = true }
                        }
                    }
                }
            }
        });

        var answerJson = JsonSerializer.Serialize(new { field = "John Doe" });

        _mockUnitOfWork.Setup(u => u.QuestionRepository.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                QuestionContent = questionContent,
                TaskId = Guid.NewGuid(),
                CurrentQuestionNameUrl = "q1",
                QuestionTypeName = "TextInputGroup",
                TaskNameUrl = "task"
            });

        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository.CheckIfQuestionAnswerExists(questionId, "field", "John Doe"))
            .ReturnsAsync(true);

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, answerJson);

        // Assert
        Assert.Single(result?.Errors!);
        Assert.Equal("The Your name \"John Doe\" already exists in our records", result?.Errors!.First().ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnError_WhenAnswerTooShortByWordCount()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var questionContent = JsonSerializer.Serialize(new QuestionContent
        {
            FormGroup = new FormGroup
            {
                Textarea = new Textarea
                {
                    Name = "bio",
                    Label = new TextWithSize { Text = "Bio" },
                    Validation = new ValidationRule
                    {
                        CountWords = true,
                        MinLength = 5
                    }
                }
            }
        });

        var answerJson = JsonSerializer.Serialize(new { bio = "Too short" });

        _mockUnitOfWork.Setup(u => u.QuestionRepository.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                QuestionContent = questionContent,
                TaskId = Guid.NewGuid(),
                CurrentQuestionNameUrl = "q1",
                QuestionTypeName = "Textarea",
                TaskNameUrl = "task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, answerJson);

        // Assert
        Assert.Single(result?.Errors!);
        Assert.Equal("Bio must be 5 words or more", result?.Errors!.First().ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnError_WhenPatternDoesNotMatch()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var questionContent = JsonSerializer.Serialize(new QuestionContent
        {
            FormGroup = new FormGroup
            {
                TextInputGroup = new TextInputGroup
                {
                    Fields = new List<TextInputItem>
                    {
                        new()
                        {
                            Name = "code",
                            Label = "Reference code",
                            Validation = new ValidationRule { Pattern = "^[A-Z]{3}$" }
                        }
                    }
                }
            }
        });

        var answerJson = JsonSerializer.Serialize(new { code = "abc" });

        _mockUnitOfWork.Setup(u => u.QuestionRepository.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                QuestionContent = questionContent,
                TaskId = Guid.NewGuid(),
                CurrentQuestionNameUrl = "q1",
                QuestionTypeName = "TextInputGroup",
                TaskNameUrl = "task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, answerJson);

        // Assert
        Assert.Single(result!.Errors!);
        Assert.Equal("Enter a valid Reference code", result.Errors!.First().ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldReturnError_WhenMinSelectedNotMet()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var checkboxGroup = new CheckBoxGroup
        {
            Name = "activities",
            Heading = new TextWithSize { Text = "Activities" },
            Validation = new ValidationRule { MinSelected = 2 },
            Options = new List<CheckBoxItem>
            {
                new() { Label = "A", Value = "A" },
                new() { Label = "B", Value = "B" },
                new() { Label = "C", Value = "C" }
            }
        };

        var questionContent = JsonSerializer.Serialize(new QuestionContent
        {
            FormGroup = new FormGroup { CheckboxGroup = checkboxGroup }
        });

        var answerJson = JsonSerializer.Serialize(new { activities = new[] { "A" } });

        _mockUnitOfWork.Setup(u => u.QuestionRepository.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                QuestionContent = questionContent,
                TaskId = Guid.NewGuid(),
                CurrentQuestionNameUrl = "q1",
                QuestionTypeName = "CheckboxGroup",
                TaskNameUrl = "task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, answerJson);

        // Assert
        Assert.Single(result!.Errors!);
        Assert.Equal("Select at least 2 options for Activities", result.Errors!.First().ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidateQuestionAnswers_ShouldSkipConditionalValidation_WhenParentCheckboxNotSelected()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var conditionalField = new TextInputItem
        {
            Name = "details",
            Label = "Explain your role",
            Validation = new ValidationRule { Required = true }
        };

        var checkboxGroup = new CheckBoxGroup
        {
            Name = "roles",
            Heading = new TextWithSize { Text = "Select roles" },
            Options = new List<CheckBoxItem>
            {
                new()
                {
                    Label = "Label test",
                    Value = "moderator",
                    ConditionalInputs = new List<TextInputItem> { conditionalField }
                }
            }
        };

        var questionContent = JsonSerializer.Serialize(new QuestionContent
        {
            FormGroup = new FormGroup { CheckboxGroup = checkboxGroup }
        });

        var answerJson = JsonSerializer.Serialize(new { roles = new[] { "other" } });

        _mockUnitOfWork.Setup(u => u.QuestionRepository.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(new QuestionDetails
            {
                QuestionId = questionId,
                QuestionContent = questionContent,
                TaskId = Guid.NewGuid(),
                CurrentQuestionNameUrl = "q1",
                QuestionTypeName = "CheckboxGroup",
                TaskNameUrl = "task"
            });

        // Act
        var result = await _applicationAnswersService.ValidateQuestionAnswers(questionId, answerJson);

        // Assert
        Assert.Empty(result!.Errors!);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldReturnEmptyList_WhenNoAnswersExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(new List<SectionTaskQuestionAnswer>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldSkipSection_WhenReviewFlagIsFalse()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Non-Reviewed Section",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "Task 1",
                TaskOrderNumber = 1,
                QuestionId = Guid.NewGuid(),
                ReviewFlag = false,
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Test",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "name", Label = "Name" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object> { { "name", "Test User" } }),
                TaskNameUrl = "task-1",
                QuestionNameUrl = "question-1"
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LinkType>()))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldIncludeSection_WhenReviewFlagIsTrue()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Reviewed Section",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "Task 1",
                TaskOrderNumber = 1,
                QuestionId = questionId,
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Personal Info",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "txtName", Label = "Your name" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "txtName", "Test User" }
                }),
                TaskNameUrl = "task-1",
                QuestionNameUrl = "question-1"
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Reviewed Section", result[0].SectionName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldMergeTaskGroups_WhenSectionHeadingsMatch()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Criteria A",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrderNumber = 1,
                QuestionId = questionId1,
                ReviewFlag = true,
                QuestionNameUrl = "question-1",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Personal Info",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "name", Label = "Your name" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "name", "Alice" }
                })
            },
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Criteria A",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrderNumber = 1,
                QuestionId = questionId2,
                ReviewFlag = true,
                QuestionNameUrl = "question-2",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        Textarea = new Textarea
                        {
                            SectionName = "Personal Info",
                            Name = "experience",
                            Label = new TextWithSize { Text = "Describe your experience" }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "experience", "Lots" }
                })
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, It.IsAny<Guid>(), LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var section = result.First();
        Assert.Single(section.TaskGroups);
        var group = section.TaskGroups.First();
        Assert.Equal("Personal Info", group.SectionHeading);
        Assert.Equal(2, group.QuestionAnswers.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldNotMergeTaskGroups_WhenSectionHeadingsDiffer()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Criteria A",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrderNumber = 1,
                QuestionId = questionId1,
                ReviewFlag = true,
                QuestionNameUrl = "question-1",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Personal Info A",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "name", Label = "Your name" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "name", "Alice" }
                })
            },
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Criteria A",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrderNumber = 1,
                QuestionId = questionId2,
                ReviewFlag = true,
                QuestionNameUrl = "question-2",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Personal Info B",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "email", Label = "Your email" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "email", "alice@example.com" }
                })
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, It.IsAny<Guid>(), LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var section = result.First();
        Assert.Equal(2, section.TaskGroups.Count);
        Assert.Contains(section.TaskGroups, g => g.SectionHeading == "Personal Info A");
        Assert.Contains(section.TaskGroups, g => g.SectionHeading == "Personal Info B");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldHandleMultipleSections_WithCorrectOrdering()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        var sectionId1 = Guid.NewGuid();
        var sectionId2 = Guid.NewGuid();

        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId2,
                SectionName = "Section B",
                SectionOrderNumber = 2,
                TaskId = taskId2,
                TaskName = "Task 2",
                TaskOrderNumber = 1,
                QuestionId = Guid.NewGuid(),
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Group B",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "field2", Label = "Field 2" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "field2", "Answer B" }
                }),
                TaskNameUrl = "task-2",
                QuestionNameUrl = "question-2"
            },
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId1,
                SectionName = "Section A",
                SectionOrderNumber = 1,
                TaskId = taskId1,
                TaskName = "Task 1",
                TaskOrderNumber = 1,
                QuestionId = Guid.NewGuid(),
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Group A",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "field1", Label = "Field 1" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "field1", "Answer A" }
                }),
                TaskNameUrl = "task-1",
                QuestionNameUrl = "question-1"
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId1))
            .ReturnsAsync(answers.Where(a => a.TaskId == taskId1).ToList());

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId2))
            .ReturnsAsync(answers.Where(a => a.TaskId == taskId2).ToList());

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, It.IsAny<Guid>(), LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Section A", result[0].SectionName);
        Assert.Equal("Section B", result[1].SectionName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldHandleMultipleTasksWithinSection()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Shared Section",
                SectionOrderNumber = 1,
                TaskId = taskId1,
                TaskName = "Task A",
                TaskOrderNumber = 1,
                QuestionId = questionId1,
                ReviewFlag = true,
                QuestionNameUrl = "question-1",
                TaskNameUrl = "task-a",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Details A",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "inputA", Label = "Input A" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "inputA", "Value A" }
                })
            },
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Shared Section",
                SectionOrderNumber = 1,
                TaskId = taskId2,
                TaskName = "Task B",
                TaskOrderNumber = 2,
                QuestionId = questionId2,
                ReviewFlag = true,
                QuestionNameUrl = "question-2",
                TaskNameUrl = "task-b",
                QuestionContent = JsonSerializer.Serialize(new QuestionContent
                {
                    FormGroup = new FormGroup
                    {
                        TextInputGroup = new TextInputGroup
                        {
                            SectionName = "Details B",
                            Fields = new List<TextInputItem>
                            {
                                new TextInputItem { Name = "inputB", Label = "Input B" }
                            }
                        }
                    }
                }),
                Answer = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "inputB", "Value B" }
                })
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId1))
            .ReturnsAsync(answers.Where(a => a.TaskId == taskId1).ToList());

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId2))
            .ReturnsAsync(answers.Where(a => a.TaskId == taskId2).ToList());

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, It.IsAny<Guid>(), LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var section = result.First();
        Assert.Equal("Shared Section", section.SectionName);
        Assert.Equal(2, section.TaskGroups.Count);
        Assert.Contains(section.TaskGroups, g => g.SectionHeading == "Details A");
        Assert.Contains(section.TaskGroups, g => g.SectionHeading == "Details B");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldAddQuestionAnswers_ForAllControlTypes()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var attachments = new List<Attachment>
        {
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = "CV.pdf",
                BlobId = Guid.NewGuid(),
                FileMIMEtype = "application/pdf",
                FileSize = 1024,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                ModifiedDate = DateTime.UtcNow,
                CreatedByUpn = "user1",
                ModifiedByUpn = "user1"
            }
        };

        var content = new QuestionContent
        {
            FormGroup = new FormGroup
            {
                TextInputGroup = new TextInputGroup
                {
                    SectionName = "Profile",
                    Fields = new List<TextInputItem>
                {
                    new TextInputItem { Name = "fullName", Label = "Full name" }
                }
                },
                Textarea = new Textarea
                {
                    SectionName = "Profile",
                    Name = "bio",
                    Label = new TextWithSize { Text = "Bio" }
                },
                RadioButtonGroup = new RadioButtonGroup
                {
                    SectionName = "Profile",
                    Name = "role",
                    Heading = new TextWithSize { Text = "Choose role" },
                    Options = new List<RadioButtonItem>
                {
                    new RadioButtonItem { Label = "Admin", Value = "admin" }
                }
                },
                CheckboxGroup = new CheckBoxGroup
                {
                    SectionName = "Profile",
                    Name = "skills",
                    Heading = new TextWithSize { Text = "Your skills" },
                    Options = new List<CheckBoxItem>
                {
                    new CheckBoxItem
                    {
                        Label = "Leadership",
                        Value = "leadership",
                        ConditionalInputs = new List<TextInputItem>
                        {
                            new TextInputItem { Name = "leadershipDetails", Label = "Details" }
                        }
                    }
                }
                },
                FileUpload = new FileUpload
                {
                    SectionName = "Profile",
                    Label = new TextWithSize { Text = "Upload your CV" },
                    Name = "cv"
                }
            }
        };

        var answer = new Dictionary<string, object>
        {
            { "fullName", "test test" },
            { "bio", "Experienced professional" },
            { "role", "admin" },
            { "skills", new[] { "leadership" } },
            { "leadershipDetails", "test details" }
        };

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Profile Section",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "User Profile",
                TaskOrderNumber = 1,
                TaskNameUrl = "user-profile",
                QuestionId = questionId,
                QuestionNameUrl = "profile",
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(content),
                Answer = JsonSerializer.Serialize(answer),
                ApplicationId = applicationId
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(attachments);

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var group = result.First().TaskGroups.First();
        Assert.Equal(6, group.QuestionAnswers.Count);

        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Full name" && q.AnswerValue?.First() == "test test");
        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Bio" && q.AnswerValue?.First() == "Experienced professional");
        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Choose role" && q.AnswerValue?.First() == "admin");
        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Your skills" && q.AnswerValue?.First() == "leadership");
        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Details" && q.AnswerValue?.First() == "test details");
        Assert.Contains(group.QuestionAnswers, q => q.QuestionText == "Files you uploaded" && q.AnswerValue?.First() == "CV.pdf");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldSortAttachmentsAlphabetically_ByFileName()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var attachments = new List<Attachment>
        {
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = "Zebra.docx",
                BlobId = Guid.NewGuid(),
                FileMIMEtype = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileSize = 2000,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                ModifiedDate = DateTime.UtcNow,
                CreatedByUpn = "user1",
                ModifiedByUpn = "user1"
            },
            new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = "Alpha.pdf",
                BlobId = Guid.NewGuid(),
                FileMIMEtype = "application/pdf",
                FileSize = 1500,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                ModifiedDate = DateTime.UtcNow,
                CreatedByUpn = "user2",
                ModifiedByUpn = "user2"
            }
        };

        var content = new QuestionContent
        {
            FormGroup = new FormGroup
            {
                FileUpload = new FileUpload
                {
                    SectionName = "Docs",
                    Label = new TextWithSize { Text = "Files you uploaded" },
                    Name = "fileUpload"
                }
            }
        };

        var answer = new Dictionary<string, object>();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Documents",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "Upload",
                TaskOrderNumber = 1,
                TaskNameUrl = "upload",
                QuestionId = questionId,
                QuestionNameUrl = "docs",
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(content),
                Answer = JsonSerializer.Serialize(answer),
                ApplicationId = applicationId
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(attachments);

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        var fileAnswer = result
            .First().TaskGroups
            .First().QuestionAnswers
            .FirstOrDefault(q => q.QuestionText == "Files you uploaded");

        Assert.NotNull(fileAnswer);
        Assert.Equal(2, fileAnswer!.AnswerValue!.Count);
        Assert.Equal("Alpha.pdf", fileAnswer.AnswerValue[0]);
        Assert.Equal("Zebra.docx", fileAnswer.AnswerValue[1]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldAddNotProvided_WhenNoAttachmentsExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var content = new QuestionContent
        {
            FormGroup = new FormGroup
            {
                FileUpload = new FileUpload
                {
                    SectionName = "Documents",
                    Label = new TextWithSize { Text = "Upload your documents" },
                    Name = "supportingDocs"
                }
            }
        };

        var answer = new Dictionary<string, object>();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Document Upload",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "Evidence Task",
                TaskOrderNumber = 1,
                TaskNameUrl = "evidence-task",
                QuestionId = questionId,
                QuestionNameUrl = "upload-docs",
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(content),
                Answer = JsonSerializer.Serialize(answer),
                ApplicationId = applicationId
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        var fileAnswer = result
            .First().TaskGroups
            .First().QuestionAnswers
            .FirstOrDefault(q => q.QuestionText == "Files you uploaded");

        Assert.NotNull(fileAnswer);
        Assert.Single(fileAnswer!.AnswerValue!);
        Assert.Equal("Not provided", fileAnswer.AnswerValue!.First());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllApplicationAnswerReview_ShouldIgnoreTask_WhenNoValidQuestionAnswersExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var content = new QuestionContent
        {
            FormGroup = new FormGroup()
        };

        var answer = new Dictionary<string, object>();

        var answers = new List<SectionTaskQuestionAnswer>
        {
            new SectionTaskQuestionAnswer
            {
                SectionId = sectionId,
                SectionName = "Section With Empty Task",
                SectionOrderNumber = 1,
                TaskId = taskId,
                TaskName = "Empty Task",
                TaskOrderNumber = 1,
                TaskNameUrl = "empty-task",
                QuestionId = questionId,
                QuestionNameUrl = "question-1",
                ReviewFlag = true,
                QuestionContent = JsonSerializer.Serialize(content),
                Answer = JsonSerializer.Serialize(answer),
                ApplicationId = applicationId
            }
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(answers);

        _mockAttachmentRepository
            .Setup(repo => repo.GetAllAttachmentsForLink(applicationId, questionId, LinkType.Question))
            .ReturnsAsync(new List<Attachment>());

        // Act
        var result = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var section = result.First();
        Assert.Empty(section.TaskGroups);
    }
}