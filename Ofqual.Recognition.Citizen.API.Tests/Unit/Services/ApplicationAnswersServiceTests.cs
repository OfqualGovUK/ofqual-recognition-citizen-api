using Moq;

using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ofqual.Recognition.Citizen.API.Tests.Unit.Services;

public class ApplicationAnswersServiceTests
{


    public ApplicationAnswersServiceTests()
    {


    }



    [Fact]
    public async Task ValidateQuestionAnswers_AddressForm_ReturnsOK()
    {
        //Arrange
        var answerDto = new QuestionAnswerSubmissionDto()
        {
            Answer = "{ \"fullName\": \"TEST FRIDAY\", \"email\": \"TEST FRIDAY\", \"phoneNumber\": \"TEST FRIDAY\", \"jobRole\": \"TEST FRIDAY\" }" 
        };

        var questionDetails = new QuestionDetails
        {
            TaskNameUrl = "",
            QuestionTypeName = "",
            CurrentQuestionNameUrl = "",
            NextQuestionNameUrl = "",
            QuestionContent = @"{
                        ""heading"": ""Contact details"",
                        ""formGroup"": {
                            ""TextInput"": {
                                ""TextInputs"": [
                                    {
                                        ""name"": ""fullName"",
                                        ""label"": ""Full name"",
                                        ""disabled"": false,
                                        ""validation"": {
                                            ""required"": true
                                        }
                                    },
                                    {
                                        ""name"": ""email"",
                                        ""label"": ""Email address"",
                                        ""disabled"": false,
                                        ""validation"": {
                                            ""required"": true
                                        }
                                    },
                                    {
                                        ""name"": ""phoneNumber"",
                                        ""label"": ""Phone number"",
                                        ""hint"": ""For international numbers include the country code"",
                                        ""disabled"": false,
                                        ""validation"": {
                                            ""required"": true
                                        }
                                    },
                                    {
                                        ""name"": ""jobRole"",
                                        ""label"": ""Your role in the organisation"",
                                        ""hint"": ""If you are the proposed responsible officer put 'Responsible officer'. If not, put your job heading."",
                                        ""disabled"": false,
                                        ""validation"": {
                                            ""required"": true
                                        }
                                    }
                                ]
                            }
                        }
                    }"
        };

        var mockQuestion = new Mock<IQuestionRepository>();

        mockQuestion
            .Setup(x => x.GetQuestion(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(questionDetails);
            

        mockQuestion
            .Setup(x => x.CheckIfQuestionAnswerExists(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var mockUow = new Mock<IUnitOfWork>();
        mockUow
            .Setup(x => x.QuestionRepository)
            .Returns(mockQuestion.Object);

        var applicationAnswersService = new ApplicationAnswersService(mockUow.Object);

        //Act
        var result = await applicationAnswersService.ValidateQuestionAnswers(Guid.NewGuid(), Guid.NewGuid(), answerDto);

        //Assert
        //Assert.NotNull(result);
       // Assert.Empty(result);
    }
}

