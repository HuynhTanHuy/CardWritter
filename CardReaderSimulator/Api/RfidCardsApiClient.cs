using System.Net.Http.Headers;
using System.Net.Http.Json;
using CardReaderSimulator.Models;
using CardReaderSimulator.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardReaderSimulator.Api;

public sealed class RfidCardsApiClient : IRfidCardsApiClient
{
    private readonly HttpClient _http;
    private readonly IOptions<CardReaderOptions> _options;
    private readonly ILogger<RfidCardsApiClient> _logger;

    public RfidCardsApiClient(HttpClient http, IOptions<CardReaderOptions> options, ILogger<RfidCardsApiClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<(bool Success, int StatusCode, string? ReasonPhrase)> PostCardAsync(
        string rfidCardNumber,
        CancellationToken cancellationToken)
    {
        var o = _options.Value;
        if (string.IsNullOrWhiteSpace(o.BearerToken))
            _logger.LogWarning("CardReader:BearerToken chưa cấu hình — request gửi không có tiêu đề Authorization.");
        var body = new RfidCardWriteRequest
        {
            HospitalId = o.HospitalId,
            RfidCardNumber = rfidCardNumber,
            RfidCardTypeId = o.RfidCardTypeId,
            RfidCardBatchId = null,
            Status = 4,
            IsActive = true
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/rfid/cards")
        {
            Content = JsonContent.Create(body)
        };

        var token = o.BearerToken;
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var ok = response.IsSuccessStatusCode;
        if (!ok)
        {
            var errorBody = await ReadErrorBodyAsync(response, cancellationToken).ConfigureAwait(false);
            var wwwAuth = string.Join(", ", response.Headers.WwwAuthenticate.Select(h => h.ToString()));
            if (string.IsNullOrWhiteSpace(wwwAuth))
                wwwAuth = "(không có)";

            _logger.LogWarning(
                "POST api/rfid/cards thất bại: HTTP {Status} {Reason}. WWW-Authenticate: {WwwAuth}. Nội dung phản hồi: {Body}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                wwwAuth,
                string.IsNullOrEmpty(errorBody) ? "(rỗng)" : errorBody);
        }

        return (ok, (int)response.StatusCode, response.ReasonPhrase);
    }

    private static async Task<string?> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var s = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(s))
                return null;
            const int max = 2000;
            return s.Length <= max ? s : s[..max] + "…";
        }
        catch
        {
            return "(không đọc được body)";
        }
    }
}
