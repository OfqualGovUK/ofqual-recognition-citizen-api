using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Attributes;
using Microsoft.AspNetCore.Http.Features;
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

    public async Task Invoke(HttpContext context, IApplicationService applicationService)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint; // Get the endpoint being called
        var attribute = endpoint?.Metadata.GetMetadata<CheckApplicationId>(); // Get the attribute if available

        if (attribute != null && attribute.QueryParam != null)
        {
            // Attribute has been set; perform Application Check
            RouteValueDictionary routeValues = context.GetRouteData().Values;
            string? applicationIdValue = routeValues[attribute.QueryParam] as string;

            if (!Guid.TryParse(applicationIdValue, out Guid applicationId))
            {
                throw new ArgumentException("Invalid or missing ApplicationId when using CheckApplicationId attribute.");
            }

            bool canAccess = await applicationService.CheckUserCanModifyApplication(applicationId);
            if (!canAccess)
            {
                // Return a 403 Forbidden if not allowed to access
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("User does not have permission to modify or access this application");
                return;
            }
        }

        await _next(context);
    }
}
