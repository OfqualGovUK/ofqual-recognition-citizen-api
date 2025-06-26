using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
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
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository;
    private readonly Mock<IAttachmentRepository> _mockAttachmentRepository;
    private readonly ApplicationAnswersService _applicationAnswersService;

    public ApplicationAnswersServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);

        _mockApplicationAnswersRepository = new Mock<IApplicationAnswersRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);

        _mockAttachmentRepository = new Mock<IAttachmentRepository>();
        _mockUnitOfWork.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepository.Object);

        _applicationAnswersService = new ApplicationAnswersService(_mockUnitOfWork.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SavePreEngagementAnswers_ShouldReturnTrue_WhenAllAnswersAreValidAndSaved()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerJson = "{\"field\":\"value\"}";

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                        {
                            new TextInputItem
                            {
                                Name = "field",
                                Label = "Field",
                                Validation = new ValidationRule { Required = true }
                            }
                        }
                    }
                }
            }),
            CurrentQuestionNameUrl = "question-url",
            QuestionTypeName = "TextInputGroup",
            TaskNameUrl = "task-url"
        };

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = answerJson
            }
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(questionDetails);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.UpsertQuestionAnswer(applicationId, questionId, answerJson))
            .ReturnsAsync(true);

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

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                        {
                            new TextInputItem
                            {
                                Name = "field",
                                Label = "Field",
                                Validation = new ValidationRule { Required = true }
                            }
                        }
                    }
                }
            }),
            CurrentQuestionNameUrl = "question-url",
            QuestionTypeName = "TextInputGroup",
            TaskNameUrl = "task-url"
        };

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = answerJson
            }
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(questionDetails);

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

        var questionDetails = new QuestionDetails
        {
            QuestionId = questionId,
            TaskId = Guid.NewGuid(),
            QuestionContent = JsonSerializer.Serialize(new QuestionContent
            {
                FormGroup = new FormGroup
                {
                    TextInputGroup = new TextInputGroup
                    {
                        Fields = new List<TextInputItem>
                        {
                            new TextInputItem
                            {
                                Name = "field",
                                Label = "Field",
                                Validation = new ValidationRule { Required = true }
                            }
                        }
                    }
                }
            }),
            CurrentQuestionNameUrl = "question-url",
            QuestionTypeName = "TextInputGroup",
            TaskNameUrl = "task-url"
        };

        var preEngagementAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto
            {
                QuestionId = questionId,
                AnswerJson = answerJson
            }
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetQuestionByQuestionId(questionId))
            .ReturnsAsync(questionDetails);

        _mockApplicationAnswersRepository
            .Setup(repo => repo.UpsertQuestionAnswer(applicationId, questionId, answerJson))
            .ReturnsAsync(false);

        // Act
        var result = await _applicationAnswersService.SavePreEngagementAnswers(applicationId, preEngagementAnswers);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskAnswerReview_ShouldReturnSectionedAnswers_ForAllControlTypes()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var attachments = new List<Attachment>
        {
            new Attachment { FileName = "Zeta.pdf", FileMIMEtype = "application/pdf", CreatedByUpn = "user1", ModifiedByUpn = "user1" },
            new Attachment { FileName = "Alpha.pdf", FileMIMEtype = "application/pdf", CreatedByUpn = "user2", ModifiedByUpn = "user2" }
        };

        var taskQuestionAnswers = new List<TaskQuestionAnswer>
        {
            new TaskQuestionAnswer
            {
                ApplicationId = applicationId,
                TaskId = taskId,
                TaskName = "About You",
                TaskNameUrl = "about-you",
                TaskOrder = 1,
                SectionId = Guid.NewGuid(),
                SectionName = "Personal Information",
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
        Assert.Equal(5, result.Count);

        var fileSection = result.FirstOrDefault(s => s.SectionHeading == "Supporting Documents");
        Assert.NotNull(fileSection);

        var fileAnswer = fileSection!.QuestionAnswers.FirstOrDefault();
        Assert.NotNull(fileAnswer);
        Assert.Equal("Files you uploaded", fileAnswer!.QuestionText);

        var fileNames = fileAnswer.AnswerValue!;
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
        Assert.NotNull(result.Errors);
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
        Assert.Empty(result.Errors!);
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
        Assert.Single(result.Errors!);
        Assert.Equal("The Your name \"John Doe\" already exists in our records", result.Errors!.First().ErrorMessage);
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
        Assert.Single(result.Errors!);
        Assert.Equal("Bio must be 5 words or more", result.Errors!.First().ErrorMessage);
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
        Assert.Single(result.Errors!);
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
        Assert.Single(result.Errors!);
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
        Assert.Empty(result.Errors!);
    }
}