using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TRElectrosur.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = "http://localhost:3000"; // URL base del API
        }

        public async Task<T> PostAsync<T>(string endpoint, object data, string token = null)
        {
            try
            {
                // Configurar correctamente los headers
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(data, jsonOptions);
                Console.WriteLine($"Enviando datos: {jsonContent}"); // Para depuración

                var content = new StringContent(
                    jsonContent,
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta recibida: {responseContent}, StatusCode: {response.StatusCode}"); // Para depuración

                // Verificar si la solicitud fue exitosa (incluye 201 Created)
                if (response.IsSuccessStatusCode)
                {
                    // Si la respuesta está vacía o no es un JSON válido y es un código de éxito, devolvemos un objeto por defecto
                    if (string.IsNullOrWhiteSpace(responseContent) || responseContent == "{}")
                    {
                        // Si el tipo T es object, devolver un objeto simple
                        var typeofT = typeof(T);
                        if (typeofT == typeof(object))
                        {
                            return (T)(object)new { success = true };
                        }
                        return default;
                    }

                    return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                Console.WriteLine($"Error HTTP: {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PostAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetAsync<T>(string endpoint, string token = null)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta API para {endpoint}: {responseContent}"); // Para depuración

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error HTTP: {response.StatusCode}, Contenido: {responseContent}");
                    return default;
                }

                // Opciones de deserialización con PropertyNameCaseInsensitive para manejar las mayúsculas/minúsculas
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<T>(responseContent, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data, string token = null)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                StringContent content = null;
                if (data != null)
                {
                    var jsonContent = JsonSerializer.Serialize(data, jsonOptions);
                    Console.WriteLine($"Enviando datos PUT: {jsonContent}");

                    content = new StringContent(
                        jsonContent,
                        Encoding.UTF8,
                        "application/json");
                }

                var response = await _httpClient.PutAsync($"{_baseUrl}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Respuesta PUT: {responseContent}, StatusCode: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(responseContent) || responseContent == "{}")
                    {
                        var typeofT = typeof(T);
                        if (typeofT == typeof(object))
                        {
                            return (T)(object)new { success = true };
                        }
                        return default;
                    }

                    return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                Console.WriteLine($"Error HTTP: {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PutAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> DeleteAsync<T>(string endpoint, string token = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.DeleteAsync($"{_baseUrl}{endpoint}");

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error HTTP: {response.StatusCode}");
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en DeleteAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetStringAsync(string endpoint, string token = null)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetStringAsync: {ex.Message}");
                throw;
            }
        }
    }
}