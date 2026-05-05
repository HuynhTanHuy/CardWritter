using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace CardWriter.Services
{
    public sealed class HttpCardApiClient : ICardApiClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _bearerToken;
        private readonly string _rfidCardTypeId;

        public HttpCardApiClient(HttpClient http, string baseUrl, string bearerToken, string rfidCardTypeId)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
            _bearerToken = (bearerToken ?? "").Trim();
            _rfidCardTypeId = (rfidCardTypeId ?? "").Trim();
        }

        public CardApiResult CreateOrUpdateCard(string hospitalId, string rfidCardNumber, string rfidCardBatchCode)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return new CardApiResult { Success = false, Message = "Thiếu CardApiBaseUrl." };
            if (string.IsNullOrWhiteSpace(_bearerToken))
                return new CardApiResult { Success = false, Message = "Thiếu CardApiBearerToken." };
            if (string.IsNullOrWhiteSpace(_rfidCardTypeId))
                return new CardApiResult { Success = false, Message = "Thiếu RfidCardTypeId." };
            if (string.IsNullOrWhiteSpace(hospitalId))
                return new CardApiResult { Success = false, Message = "Thiếu hospitalId." };
            if (string.IsNullOrWhiteSpace(rfidCardNumber))
                return new CardApiResult { Success = false, Message = "Thiếu rfidCardNumber." };
            if (string.IsNullOrWhiteSpace(rfidCardBatchCode))
                return new CardApiResult { Success = false, Message = "Thiếu rfidCardBatchCode (Lô)." };

            var body = new
            {
                hospitalId = hospitalId,
                rfidCardNumber = rfidCardNumber,
                rfidCardTypeId = _rfidCardTypeId,
                rfidCardBatchCode = rfidCardBatchCode,
                status = 4,
                isActive = true
            };

            var json = JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/rfid/cards");
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            request.Headers.TryAddWithoutValidation("Authorization", NormalizeBearer(_bearerToken));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using (request)
                using (var response = _http.SendAsync(request).GetAwaiter().GetResult())
                {
                    var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return new CardApiResult
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        Message = string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase : text
                    };
                }
            }
            catch (Exception ex)
            {
                return new CardApiResult { Success = false, Message = ex.Message };
            }
        }

        private static string NormalizeBearer(string token)
        {
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return token;
            return "Bearer " + token;
        }
    }
}
