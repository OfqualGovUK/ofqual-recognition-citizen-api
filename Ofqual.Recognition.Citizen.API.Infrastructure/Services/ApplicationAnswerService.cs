using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Applications;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;
using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
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


    public async Task<IEnumerable<ValidationErrorItemDto>?> ValidateQuestionAnswers(Guid taskId, Guid questionId, QuestionAnswerSubmissionDto answerDto)
    {        

        var questionDetails = await _questionRepository.GetQuestion(taskId, questionId);
        if(questionDetails == null) 
            return null;

        var questionContent = JsonSerializer.Deserialize<QuestionContent>(questionDetails.QuestionContent);
        if (questionContent?.FormGroup?.Components == null)
            return null;

        var answerValue = JsonSerializer.Deserialize<Dictionary<string, string>>(answerDto.Answer);
        if (answerValue == null) 
            return null;


        var errors = new List<ValidationErrorItemDto>();
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
               if(await _questionRepository.CheckIfQuestionAnswerExists(taskId, questionId, answerItem.Key, answerItem.Value))
                    errors.Add(new ValidationErrorItemDto
                    {
                        Property = answerItem.Key,
                        ErrorMessage = $"The {answerItem.Key} \"{answerItem.Value}\" already exists within our records"
                    });
               continue;
            }

            if (component is TextInput)
            {
                var textLengthError = ValidateTextLength(component, answerItem);
                if (textLengthError != null)
                {
                    errors.Add(textLengthError);
                    continue;
                }
                
                if (!string.IsNullOrWhiteSpace(component.Validation.Pattern)) 
                {
                    var regex = new Regex(component.Validation.Pattern);
                    if (!regex.IsMatch(answerItem.Value))
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = $"answer does not match the required format" });
                    continue;
                }
            }

            if (component is RadioButton button)
            {
                if (component.Validation.MinSelected.HasValue)
                { 
                    if(button.Radios.Count() < component.Validation.MinSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "minimum number of items has not been selected" });
                    continue;
                }
                if (component.Validation.MaxSelected.HasValue)
                {
                    if (button.Radios.Count() < component.Validation.MaxSelected)
                        errors.Add(new ValidationErrorItemDto { Property = answerItem.Key, ErrorMessage = "too many items have been selected" });
                    continue;
                }
            }
        }
        return errors;
    }

    private static ValidationErrorItemDto? ValidateTextLength(IComponent component, KeyValuePair<string, string> answerItem)
    {
        var hasMinValue = component.Validation?.MinLength.HasValue ?? false;
        var hasMaxValue = component.Validation?.MaxLength.HasValue ?? false;

        //Skip if we dont have min or max values set
        if (!hasMinValue && !hasMaxValue)
            return null;

        //are we counting characters or words?
        var countWords = component.Validation?.CountWords ?? false;
        var answerLength = countWords
            ? answerItem.Value.Split(['\t', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries).Length
            : answerItem.Value.Length;

        //WHERE minimum value is set AND size is smaller than minimum value
        //OR maximum value is set AND size is bigger than maxiimum value
        //THEN return an error
        if ((hasMinValue && answerLength < component.Validation!.MinLength)
        ||  (hasMaxValue && answerLength > component.Validation!.MaxLength))
        {
            var itemDto = new ValidationErrorItemDto 
            { 
                Property = answerItem.Key, 
                ErrorMessage = $"{answerItem.Key} must be " 
            };

            var countType = countWords ? "words": "characters";
           
            if (!hasMinValue)
                itemDto.ErrorMessage += $"{component.Validation!.MaxLength} {countType} or more";
            else if (!hasMaxValue)
                itemDto.ErrorMessage += $"{component.Validation!.MinLength} {countType} or less";
            else
                itemDto.ErrorMessage += $"between {component.Validation!.MinLength} " 
                    + $"and {component.Validation!.MaxLength} {countType}";
            return itemDto;
        }
        return null;
    }
}

