using Newtonsoft.Json;
using System.Text;

namespace MqttListener
{
    class FrigateApiClient(string uriBase, string username, string password)
    {
        public async Task<string> GenerateAuthenticationTokenAsync()
        {
            // Fetch preview as a gif
            using var httpClient = new HttpClient();

            var authUrl = $"{uriBase}/login";
            var payload = new
            {
                user = username,
                password,
            };

            var jsonContent = new StringContent(
               JsonConvert.SerializeObject(payload),
               Encoding.UTF8,
               "application/json"
            );

            var response = await httpClient.PostAsync(authUrl, jsonContent);

            if (response.IsSuccessStatusCode is false)
            {
                throw new Exception($"API request failed with status code {response.StatusCode}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            // Handle the response content here if needed


            if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                throw new Exception($"Could not retrieve authentication token. Response did not contain a cookie.");
            }

            // Search for the frigate_token cookie
            var frigateTokenCookie = cookies.FirstOrDefault(c => c.StartsWith("frigate_token=")) ?? throw new Exception($"Could not retrieve authentication token. Failed to parse cookie.");

            // Extract the token value from the cookie
            var frigateToken = frigateTokenCookie.Split(';')
                .FirstOrDefault(c => c.StartsWith("frigate_token="))
                ?.Split('=')[1];

            return frigateToken ?? throw new Exception($"Could not retrieve authentication token. Failed to parse cookie.");
        }

        public async Task<byte[]> FetchPreviewGifAsync(string reviewId)
        {
            // Extract the token value from the cookie
            var frigateToken = await GenerateAuthenticationTokenAsync();

            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", frigateToken);

            // Fetch the GIF
            var url = $"{uriBase}/review/{reviewId}/preview?format=gif";
            var gifBytes = await httpClient.GetByteArrayAsync(url);

            return gifBytes;
        }
    }
}