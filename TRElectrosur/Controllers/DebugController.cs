using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TRElectrosur.Models;

namespace TRElectrosur.Controllers
{
    public class DebugController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DebugController> _logger;

        public DebugController(IHttpClientFactory httpClientFactory, ILogger<DebugController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Solo disponible en desarrollo
        public IActionResult Index()
        {
            if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return NotFound();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestLogin(string email, string password)
        {
            if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return NotFound();
            }

            var result = new Dictionary<string, string>();

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Configurar cabeceras
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Crear el cuerpo de la solicitud
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password
                };

                var jsonContent = JsonSerializer.Serialize(loginRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                result.Add("requestPayload", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Enviar la solicitud
                var response = await client.PostAsync("http://localhost:3000/api/auth/login", content);

                // Obtener la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                result.Add("statusCode", response.StatusCode.ToString());
                result.Add("responseContent", responseContent);

                // Intenta deserializar para verificar
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null)
                    {
                        result.Add("deserialized", "Éxito");
                        result.Add("token", loginResponse.Token ?? "Ninguno");
                    }
                    else
                    {
                        result.Add("deserialized", "Fallo: El objeto deserializado es nulo");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add("error", ex.Message);
                result.Add("stackTrace", ex.StackTrace);
            }

            return Json(result);
        }
    }
}