using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AntiVirusService : IAntiVirusService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly AntiVirusConfiguration _config;
    private const int PollingIntervalSeconds = 5;

    public AntiVirusService(IHttpClientFactory clientFactory, AntiVirusConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    public async Task<VirusScan> ScanFile(Stream fileStream, string fileName)
    {
        using var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AuthToken);

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        form.Add(fileContent, "file", fileName);

        var initialResponse = await client.PostAsync(_config.BaseUri, form);

        if (!initialResponse.IsSuccessStatusCode)
        {
            Log.Warning("Failed to initiate virus scan for file '{FileName}'. HTTP {StatusCode}", fileName, initialResponse.StatusCode);
            return new VirusScan { IsOk = false };
        }

        var scanResult = await DeserialiseResponse(initialResponse);
        var virusScan = AntiVirusMapper.MapToVirusScan(scanResult);

        int totalWaitTime = 0;
        while (virusScan.IsPending)
        {
            await Task.Delay(PollingIntervalSeconds * 1000);
            totalWaitTime += PollingIntervalSeconds;
            virusScan = await PollScanResult(client, virusScan.ScanId);
        }

        if (!virusScan.IsOk)
        {
            Log.Warning("Virus detected in file '{FileName}' or scan failed. ScanId: {ScanId}", fileName, virusScan.ScanId);
        }

        return virusScan;
    }

    private async Task<VirusScan> PollScanResult(HttpClient client, string scanId)
    {
        var resultUrl = $"{_config.BaseUri}/{scanId}";
        var response = await client.GetAsync(resultUrl);

        if (!response.IsSuccessStatusCode)
        {
            Log.Warning("Failed to retrieve scan result for ID '{ScanId}'. HTTP {StatusCode}", scanId, response.StatusCode);
            return new VirusScan { IsOk = false, IsPending = false, ScanId = scanId };
        }

        var result = await DeserialiseResponse(response);
        return AntiVirusMapper.MapToVirusScan(result);
    }

    private static async Task<AttachmentScannerResult> DeserialiseResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<AttachmentScannerResult>(json);
        
        return result ?? throw new JsonException("Failed to deserialise scan result.");
    }
}