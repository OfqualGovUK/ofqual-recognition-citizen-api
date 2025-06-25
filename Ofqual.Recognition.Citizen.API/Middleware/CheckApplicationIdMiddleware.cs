using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Ofqual.Recognition.Citizen.API.Attributes;
using System.Net;

namespace Ofqual.Recognition.Citizen.API.Middleware;

/* 
 * Middleware that checks if a user can modify an application
 * This check only runs on endpoints that use the CheckApplicationId Attribute
 */
public class CheckApplicationIdMiddleware
{
    private RequestDelegate _next;

    public CheckApplicationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IUserInformationService userInformationService)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint; // Get the endpoint being called
        var attribute = endpoint?.Metadata.GetMetadata<CheckApplicationId>(); // Get the attribute if available
        if (attribute != null && attribute.QueryParam != null)
        {
            // Attribute has been set; perform Application Check
            RouteValueDictionary routeValues = context.GetRouteData().Values;
            string? applicationId = routeValues[attribute.QueryParam] as string; // pull out the applicationId based on the attribute
            if (applicationId == null)
            {
                throw new ArgumentException("ApplicationId was not found when using CheckApplicationId attribute");
            }
            bool canAccess = await userInformationService.CheckUserCanModifyApplication(applicationId);
            if (!canAccess) { 
                // Return a 403 Forbidden if not allowed to access
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden; // Forbidden
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("User does not have permission to modify or access this application");
                return;
            }
        }
        await _next(context);
    }
}
