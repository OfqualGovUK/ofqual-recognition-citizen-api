using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AntiVirusService : IAntiVirusService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly AntiVirusConfiguration _config;
    private const int PollingIntervalSeconds = 5;
    private const int MaxPollingDurationSeconds = 30;

    public AntiVirusService(IHttpClientFactory clientFactory, AntiVirusConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    public async Task<AttachmentScannerResult?> ScanFile(Stream fileStream, string fileName)
    {
        using var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AuthToken);

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        form.Add(fileContent, "file", fileName);

        var initialResponse = await client.PostAsync(_config.BaseUri, form);
        if (!initialResponse.IsSuccessStatusCode)
        {
            Log.Error("Failed to initiate virus scan for file '{FileName}'. HTTP {StatusCode}", fileName, initialResponse.StatusCode);
            return null;
        }

        var scanResult = await DeserialiseResponse(initialResponse);
        if (scanResult == null)
        {
            return null;
        }

        int totalWaitTime = 0;
        while (scanResult.Status == ScanStatus.Pending && totalWaitTime < MaxPollingDurationSeconds)
        {
            await Task.Delay(PollingIntervalSeconds * 1000);
            totalWaitTime += PollingIntervalSeconds;

            scanResult = await PollScanResult(client, scanResult.Id!);
            if (scanResult == null)
            {
                return null;
            }
        }

        switch (scanResult.Status)
        {
            case ScanStatus.Ok:
                Log.Information("Virus scan completed successfully for '{FileName}'. File is clean. ScanId: {ScanId}", fileName, scanResult.Id);
                break;

            case ScanStatus.Found:
                Log.Warning("Infected file detected: '{FileName}'. ScanId: {ScanId}. Matches: {Matches}", fileName, scanResult.Id, string.Join(", ", scanResult.Matches));
                break;

            case ScanStatus.Failed:
                Log.Warning("Scan failed for file '{FileName}'. ScanId: {ScanId}", fileName, scanResult.Id);
                break;

            case ScanStatus.Pending:
                Log.Warning("Virus scan timed out for file '{FileName}'. ScanId: {ScanId}", fileName, scanResult.Id);
                break;

            case ScanStatus.Warning:
                Log.Warning("Scan returned a warning for file '{FileName}'. ScanId: {ScanId}. Matches: {Matches}", fileName, scanResult.Id, string.Join(", ", scanResult.Matches));
                break;

            default:
                Log.Warning("Unknown scan status '{Status}' for file '{FileName}'. ScanId: {ScanId}", scanResult.Status, fileName, scanResult.Id);
                break;
        }

        return scanResult;
    }

    private async Task<AttachmentScannerResult?> PollScanResult(HttpClient client, string scanId)
    {
        var resultUrl = $"{_config.BaseUri}/{scanId}";
        var response = await client.GetAsync(resultUrl);

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Failed to retrieve scan result for ID '{ScanId}'. HTTP {StatusCode}", scanId, response.StatusCode);
            return null;
        }

        return await DeserialiseResponse(response);
    }

    private static async Task<AttachmentScannerResult?> DeserialiseResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AttachmentScannerResult>(json);
    }
}