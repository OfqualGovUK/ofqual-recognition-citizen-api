using Ofqual.Recognition.Citizen.API.Notification.Services.PDF;


public class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize the PDF generation process
        await GenerateSamplePDF();
    }
    private static async Task GenerateSamplePDF()
    {
        try
        {
            var htmlContent = @"
            <html>
                <head>
                    <title>Sample PDF</title>
                    <style>
                        body { font-family: Arial, sans-serif; }
                        h1 { color: #333; }
                        p { font-size: 14px; }
                    </style>
                </head>
                <body>
                    <h1>Sample PDF Document</h1>
                    <p>This is a sample PDF document generated from HTML content.</p>
            </html>";

            byte[]? pdfBytes = await PDFGenerator.GeneratePDF(htmlContent);
            if (pdfBytes == null)
            {
                Console.WriteLine("Failed to generate PDF.");
                return;
            }
            File.WriteAllBytes("sample.pdf", pdfBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while generating the PDF: {ex.Message}");
        }
    }
}

