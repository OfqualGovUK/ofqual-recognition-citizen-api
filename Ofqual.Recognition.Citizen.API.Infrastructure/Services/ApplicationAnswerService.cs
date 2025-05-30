using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Applications;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;
public class ApplicationAnswerService: IApplicationAnswerService
{
    private readonly IQuestionRepository _questionRepository;

    public ApplicationAnswerService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }


    public async Task<IEnumerable<ValidationErrorItemDto>?> ValidateQuestionAnswers(string taskNameUrl, string questionNameUrl, QuestionAnswerSubmissionDto answerDto)
    {
        var errors = new List<ValidationErrorItemDto>();

        var questionDetails = await _questionRepository.GetQuestion(taskNameUrl, questionNameUrl);
        if(questionDetails == null) 
            return null;

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent);
        if (questionContent?.FormGroup?.Components == null)
            return null;



        var answerValue = JsonSerializer.Deserialize<Dictionary<string, string>>(answerDto.Answer);
        if (answerValue == null) return null;

        foreach (var answerItem in answerValue)
        {
            var component = questionContent
                ?.FormGroup
                ?.Components
                .First(x => x.Name != null
                         && x.Name.Equals(answerItem.Key,
                                          StringComparison.CurrentCultureIgnoreCase));


            if (component?.Validation == null)
                continue;

            if (component.Validation.Required ?? false)
            {
                if(string.IsNullOrWhiteSpace(answerItem.Value))
                errors.Add(new ValidationErrorItemDto
                {
                    Property = answerItem.Key,
                    ErrorMessage = $"Enter {answerItem.Key}"
                });
                continue;
            }


            if (component.Validation.Unique ?? false)
            { 
               if(await _questionRepository.CheckIfQuestionAnswerExists(taskNameUrl, questionNameUrl, answerItem.Key, answerItem.Value))
                    errors.Add(new ValidationErrorItemDto
                    {
                        Property = answerItem.Key,
                        ErrorMessage = $"The {answerItem.Key} \"{answerItem.Value}\" already exists within our records"
                    });
               continue;
            }

            if (component is TextInput)
            {
                if (component.Validation.MinLength.HasValue)
                {
                    if (component.Validation.CountWords ?? false)
                    {

                    }
                    else
                    {
                        if(answerItem.Value.Length < component.Validation.MinLength)
                            errors.Add(new ValidationErrorItemDto 
                            { 
                                Property = answerItem.Key, 
                                ErrorMessage = $"{answerItem.Key} must be {component.Validation.MinLength} characters or more" 
                            });
                    }
                }
                if (component.Validation.MaxLength.HasValue)
                {
                    if (component.Validation.CountWords ?? false)
                    {

                    }
                    else
                    {
                        if (answerItem.Value.Length < component.Validation.MaxLength)
                            errors.Add(new ValidationErrorItemDto
                            {
                                Property = answerItem.Key,
                                ErrorMessage = $"{answerItem.Key} must be {component.Validation.MaxLength} characters or less"
                            });
                    }
                }
                if (!string.IsNullOrWhiteSpace(component.Validation.Pattern)) 
                {
                    var regex = new Regex(component.Validation.Pattern);
                    if (!regex.IsMatch(answerItem.Value))
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = $"answer does not match the required format" });

                }
            }

            if (component is RadioButton)
            {
                if (component.Validation.MinSelected.HasValue)
                { 
                    if(((RadioButton)component).Radios.Count() < component.Validation.MinSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "minimum number of items has not been selected" });
                }
                if (component.Validation.MaxSelected.HasValue)
                {
                    if (((RadioButton)component).Radios.Count() < component.Validation.MaxSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "too many items have been selected" });
                }
            }
            

        }

        return errors;
    }
}

