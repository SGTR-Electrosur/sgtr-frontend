using System.Net.Http.Headers;

namespace TRElectrosur.Extensions
{
    public static class HttpClientExtensions
    {
        public static void SetBearerToken(this HttpClient client, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}