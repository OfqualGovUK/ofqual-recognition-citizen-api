using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json;

namespace Ofqual.Recognition.Citizen.API.Notification.Services.HTML;


public static class HtmlPageGenerator
{
    /// <summary>
    /// Generates HTML content from a Razor component using the provided JSON data.
    /// </summary>
    /// <typeparam name="T">
    ///     The Razor template component to base the HTML content
    /// </typeparam>
    /// <param name="jsonData">
    ///     Dictionary of parameters to be added to the template
    /// </param>
    /// <returns>
    ///     Completed HTML data as a string
    /// </returns>
    public static async Task<string?> Generate<T>(string jsonData)
        where T : IComponent
    {
        var dataModel = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);

        try
        {
            using var htmlRenderer = await InitialiseHTMLRenderer();

            await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {               
                var htmlOutput = string.IsNullOrEmpty(jsonData) 
                    ? await htmlRenderer.RenderComponentAsync<T>()
                    : await htmlRenderer.RenderComponentAsync<T>(ParameterView.FromDictionary(dataModel!));
                return htmlOutput.ToHtmlString();
            });
        }
        catch (Exception ex)
        {            
            Log.Error(ex, "Failed to generate Application Review Page HTML", dataModel);            
        }
        return null;
    }

    private static async Task<HtmlRenderer> InitialiseHTMLRenderer()
    {
        // Set up a seperate service collection and logger factory for our HTML generation
        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(); // Add default logging context for HTML generation
        serviceCollection.AddGovUkFrontend(); // Add GovUK Frontend components for HTML rendering
        
        IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        ILoggerFactory htmlLoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return await Task.FromResult(new HtmlRenderer(serviceProvider, htmlLoggerFactory));
    }
}
