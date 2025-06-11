using Moq;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ofqual.Recognition.Citizen.API.Tests.Unit.Services.Validation;

public partial class ApplicationAnswersServiceTests
{

    [Fact]
    public async Task ValidateQuestionAnswers_AddressForm_ReturnsOK()
    {
        //Arrange
        var answerDto = new QuestionAnswerSubmissionDto()
        {
            Answer = @"{ ""fullName"": ""TEST FRIDAY"", ""email"": ""TEST FRIDAY"", ""phoneNumber"": ""TEST FRIDAY"", ""jobRole"": ""TEST FRIDAY"" }"
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
        var mockQuestion = new ApplicationServiceTestQuestionRepository(questionDetails);
        var applicationAnswersService = new ApplicationAnswersService(new ApplicationServiceTestIOW(mockQuestion));

        //Act
        var result = await applicationAnswersService.ValidateQuestionAnswers(Guid.NewGuid(), Guid.NewGuid(), answerDto);

        //Assert
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateQuestionAnswers_AddressForm_ReturnsEmptyEmailError()
    {
        //Arrange
        var answerDto = new QuestionAnswerSubmissionDto()
        {
            Answer = @"{ ""fullName"": ""TEST FRIDAY"", ""email"": """", ""phoneNumber"": ""TEST FRIDAY"", ""jobRole"": ""TEST FRIDAY"" }"
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

        var mockQuestion = new ApplicationServiceTestQuestionRepository(questionDetails);
        var applicationAnswersService = new ApplicationAnswersService(new ApplicationServiceTestIOW(mockQuestion));

        //Act
        var result = await applicationAnswersService.ValidateQuestionAnswers(Guid.NewGuid(), Guid.NewGuid(), answerDto);

        //Assert        
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ValidateQuestionAnswers_Identity_ReturnsOK()
    {
        //Arrange
        var answerDto = new QuestionAnswerSubmissionDto()
        {
            Answer = @"{
                ""typeOfOrganisation"": [
                    ""Registered with Companies House"",
                    ""Registered with the Charities Commission in England and Wales"",
                    ""Public body or higher or further education institution"",
                    ""Individual (sole trader) or a partnership (not including Limited Liability Partnerships)"",
                    ""Registered in another country""
                ],
                ""registeredCompanyNumber"": ""Coder"",
                ""registeredCharityNumber"": ""11"",
                ""otherOrganisation"": ""Public body"",
                ""registeredCountry"": ""United Kingdom"",
                ""otherCountryNumber"": ""999""
            }"
        };

        var questionDetails = new QuestionDetails
        {
            TaskNameUrl = "",
            QuestionTypeName = "",
            CurrentQuestionNameUrl = "",
            NextQuestionNameUrl = "",
            QuestionContent = @"{
                ""heading"": ""Criteria A.1 - Identity"",
                ""help"": [
                    {
                        ""links"": [
                            {
                                ""text"": ""Criteria for recognition - A.1 and A.2"",
                                ""url"": ""https://example.com""
                            },
                            {
                                ""text"": ""Guidance for the criteria for recognition - criteria A.1 and A.2"",
                                ""url"": ""https://example.com""
                            }
                        ]
                    },
                    {
                        ""content"": [
                            {
                                ""type"": ""paragraph"",
                                ""text"": ""A substantial presence is defined as...""
                            }
                        ]
                    }
                ],
                ""formGroup"": {
                    ""checkbox"": {
                        ""heading"": {
                            ""text"": ""Details of your organisation's legal entity""
                        },
                        ""validation"": 
                        { 
                            ""required"": true 
                        },
                        ""name"": ""typeOfOrganisation"",
                        ""checkBoxes"": [
                            {
                                ""label"": ""The organisation is registered with companies house"",
                                ""value"": ""Registered with Companies House"",
                                ""conditionalInputs"": [
                                    {
                                        ""label"": ""Registered company number"",
                                        ""name"": ""registeredCompanyNumber"",
                                        ""inputType"": ""text"",
                                        ""disabled"": false
                                    }
                                ]
                            },
                            {
                                ""label"": ""The organisation is registered with The Charity Commission in England and Wales"",
                                ""value"": ""Registered with the Charities Commission in England and Wales"",
                                ""conditionalInputs"": [
                                    {
                                        ""label"": ""Registered charity number"",
                                        ""name"": ""registeredCharityNumber"",
                                        ""inputType"": ""text"",
                                        ""disabled"": false
                                    }
                                ]
                            },
                            {
                                ""label"": ""The organisation is a public body or a further or higher education institution"",
                                ""value"": ""Public body or higher or further education institution"",
                                ""conditionalSelects"": [
                                    {
                                        ""label"": ""Type of organisation"",
                                        ""name"": ""otherOrganisation"",
                                        ""hint"": null,
                                        ""disabled"": false,
                                        ""options"": [
                                            {
                                                ""label"": ""Select a type"",
                                                ""value"": """",
                                                ""selected"": true
                                            },
                                            {
                                                ""label"": ""Public body"",
                                                ""value"": ""Public body"",
                                                ""selected"": false
                                            },
                                            {
                                                ""label"": ""Further or Higher education institution"",
                                                ""value"": ""Further or Higher education institution"",
                                                ""selected"": false
                                            }
                                        ]
                                    }
                                ]
                            },
                            {
                                ""label"": ""The organisation is an individual (sole trader) or a partnership (not including Limited Liability Partnerships)"",
                                ""value"": ""Individual (sole trader) or a partnership (not including Limited Liability Partnerships)""
                            },
                            {
                                ""label"": ""The organisation is registered in another country"",
                                ""value"": ""Registered in another country"",
                                ""conditionalInputs"": [
                                    {
                                        ""label"": ""Country the organisation is registered in"",
                                        ""name"": ""registeredCountry"",
                                        ""inputType"": ""text"",
                                        ""disabled"": false
                                    },
                                    {
                                        ""label"": ""Registered company number in that country"",
                                        ""name"": ""otherCountryNumber"",
                                        ""inputType"": ""text"",
                                        ""disabled"": false
                                    }
                                ]
                            }
                        ]
                    }
                }
            }"
        };
        var mockQuestion = new ApplicationServiceTestQuestionRepository(questionDetails);
        var applicationAnswersService = new ApplicationAnswersService(new ApplicationServiceTestIOW(mockQuestion));

        //Act
        var result = await applicationAnswersService.ValidateQuestionAnswers(Guid.NewGuid(), Guid.NewGuid(), answerDto);

        //Assert
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

}

