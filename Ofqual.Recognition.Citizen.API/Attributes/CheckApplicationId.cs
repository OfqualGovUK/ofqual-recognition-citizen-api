namespace Ofqual.Recognition.Citizen.API.Attributes;

/* 
 * This attribute is used to indicate to middleware that it should perform an ApplicationId check 
 * 
 * Use this attribute whenever a client makes a request related to a specific application. This should only be used in Controller endpoints
 * 
 * You must specify as a manual attribute the queryParameter that corresponds to the ApplicationId
 */
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CheckApplicationId : Attribute
{
    public string QueryParam { get; set; }

    public CheckApplicationId(string queryParam)
    {
        QueryParam = queryParam;
    }
}
