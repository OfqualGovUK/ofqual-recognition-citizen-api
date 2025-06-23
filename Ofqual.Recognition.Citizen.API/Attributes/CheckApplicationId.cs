namespace Ofqual.Recognition.Citizen.API.Attributes;

/* This attribute is used to indicate to middleware that it should perform an ApplicationId check */
public class CheckApplicationId : Attribute
{
    public string QueryParam { get; set; }

    public CheckApplicationId(string queryParam)
    {
        QueryParam = queryParam;
    }
}
