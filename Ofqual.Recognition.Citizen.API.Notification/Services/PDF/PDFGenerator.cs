using Microsoft.Playwright;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Notification.Services.PDF;

public static class PDFGenerator
{
    /// <summary>
    ///     Uses Playwright to generate a PDF from the provided HTML content, using an installed headless chromium browser.
    /// </summary>
    /// <param name="htmlContent">
    ///     The HTML content to convert to PDF.
    /// </param>
    /// <param name="pdfOptions">
    ///     Optional: Generate PDF using the provided PagePdfOptions, 
    ///     see https://playwright.dev/dotnet/docs/api/class-page#page-pdf for further information </param>
    /// <param name="emulateMediaOptions">
    ///     Optional: allow changes of the CSS media type, 
    ///     see https://playwright.dev/dotnet/docs/api/class-page#page-emulate-media for further information</param>
    /// <returns>Payload data of the PDF Generated from the HTML file</returns>
    public static async Task<byte[]?> GeneratePDF(
        string htmlContent,
        PagePdfOptions? pdfOptions = null,
        PageEmulateMediaOptions? emulateMediaOptions = null)
    {
        if (!string.IsNullOrWhiteSpace(htmlContent))
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

                var page = await browser.NewPageAsync();
                await page.SetContentAsync(htmlContent);

                if (emulateMediaOptions != null)
                    await page.EmulateMediaAsync(emulateMediaOptions);

                var pdfBytes = await page.PdfAsync(pdfOptions ?? new() { Format = "A4" });
                await page.CloseAsync();
                return pdfBytes;
            }
            catch (PlaywrightException pEx)
            {
                Log.Error(pEx, "GeneratePDF: Playwright error occurred, this may be due to not having the correct browser installed");               
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GeneratePDF: An error occurred while generating PDF");
            }
        }
        return null;
    }
}
