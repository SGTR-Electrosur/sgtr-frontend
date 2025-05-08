using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRElectrosur.Filters;
using TRElectrosur.Models;
using TRElectrosur.Services;

namespace TRElectrosur.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(ApiService apiService, AuthService authService, ILogger<UsuariosController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                string token = _authService.GetToken();

                // Obtener los usuarios
                var users = await _apiService.GetAsync<List<User>>("/api/users", token);

                // Obtener las áreas (igual que en Crear y Editar)
                var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);
                ViewBag.Areas = areas ?? new List<Area>();

                return View(users ?? new List<User>());
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al obtener usuarios: {ex.Message}");
                ViewBag.ErrorMessage = "No se pudieron cargar los usuarios";
                return View(new List<User>());
            }
        }
        public async Task<IActionResult> Crear()
        {
            try
            {
                string token = _authService.GetToken();
                var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);

                ViewBag.Areas = areas ?? new List<Area>();

                return View();
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al cargar áreas: {ex.Message}");
                ViewBag.Areas = new List<Area>();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Crear(string firstName, string lastName, string email,
                                            string password, int roleId, int areaId, bool isActive = true)
        {
            try
            {
                string token = _authService.GetToken();

                var request = new
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Password = password,
                    RoleId = roleId,
                    AreaId = areaId
                };

                // Uso del HttpClient directamente para tener más control sobre la respuesta
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    if (!string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(request, jsonOptions);
                    _logger.LogInformation($"Enviando datos: {jsonContent}");

                    var content = new StringContent(
                        jsonContent,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync("http://localhost:3000/api/auth/register", content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Respuesta recibida: {responseContent}, Status: {response.StatusCode}");

                    // Considera éxito incluso con 201 Created
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Usuario creado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Tratar de entender el error
                        string errorMessage = "No se pudo crear el usuario";
                        try
                        {
                            var errorResponse = System.Text.Json.JsonDocument.Parse(responseContent);
                            if (errorResponse.RootElement.TryGetProperty("message", out var messageElement))
                            {
                                errorMessage = messageElement.GetString() ?? errorMessage;
                            }
                        }
                        catch { }

                        ViewBag.ErrorMessage = errorMessage;
                        var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);
                        ViewBag.Areas = areas ?? new List<Area>();
                        return View();
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al crear usuario: {ex.Message}");
                ViewBag.ErrorMessage = "Error al procesar la solicitud";

                try
                {
                    string token = _authService.GetToken();
                    var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);
                    ViewBag.Areas = areas ?? new List<Area>();
                }
                catch
                {
                    ViewBag.Areas = new List<Area>();
                }

                return View();
            }
        }

        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                string token = _authService.GetToken();

                var user = await _apiService.GetAsync<User>($"/api/users/{id}", token);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index");
                }

                var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);
                ViewBag.Areas = areas ?? new List<Area>();

                // Agregar roles para la vista
                ViewBag.Roles = new List<object>
                {
                    new { Value = "1", Text = "Administrador", Selected = user.RoleID == 1 },
                    new { Value = "2", Text = "Usuario", Selected = user.RoleID == 2 }
                };

                return View(user);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al obtener usuario {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los datos del usuario";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, string firstName, string lastName, string email,
                                                     int roleId, int? areaId, bool isActive = true)
        {
            try
            {
                string token = _authService.GetToken();

                // Verificar valores de isActive (puede ser que el checkbox no envíe valor si no está marcado)
                // Si no viene en el formulario, debemos asegurarnos que sea false

                _logger.LogInformation($"Editando usuario ID {id}: Email={email}, FirstName={firstName}, LastName={lastName}, RoleId={roleId}, AreaId={areaId}, IsActive={isActive}");

                // Usar HttpClient directamente para mayor control
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    if (!string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    var request = new
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        RoleId = roleId,
                        AreaId = areaId,
                        IsActive = isActive
                    };

                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(request, jsonOptions);
                    _logger.LogInformation($"Enviando datos: {jsonContent}");

                    var content = new StringContent(
                        jsonContent,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PutAsync($"http://localhost:3000/api/users/{id}", content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Respuesta recibida: {responseContent}, Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Usuario actualizado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        string errorMessage = "No se pudo actualizar el usuario";
                        try
                        {
                            var errorResponse = System.Text.Json.JsonDocument.Parse(responseContent);
                            if (errorResponse.RootElement.TryGetProperty("message", out var messageElement))
                            {
                                errorMessage = messageElement.GetString() ?? errorMessage;
                            }
                        }
                        catch { }

                        ViewBag.ErrorMessage = errorMessage;

                        var user = await _apiService.GetAsync<User>($"/api/users/{id}", token);
                        var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);

                        ViewBag.Areas = areas ?? new List<Area>();

                        return View(user);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al actualizar usuario {id}: {ex.Message}");
                ViewBag.ErrorMessage = "Error al procesar la solicitud";

                try
                {
                    string token = _authService.GetToken();
                    var user = await _apiService.GetAsync<User>($"/api/users/{id}", token);
                    var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);

                    ViewBag.Areas = areas ?? new List<Area>();

                    return View(user);
                }
                catch
                {
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(int id, string newPassword)
        {
            try
            {
                string token = _authService.GetToken();

                var request = new { newPassword = newPassword };
                var response = await _apiService.PutAsync<dynamic>($"/api/users/{id}/password", request, token);

                TempData["SuccessMessage"] = "Contraseña actualizada correctamente";
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al cambiar contraseña de usuario {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cambiar la contraseña";
            }

            return RedirectToAction("Editar", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                string token = _authService.GetToken();

                var response = await _apiService.PutAsync<dynamic>($"/api/users/{id}/deactivate", null, token);

                TempData["SuccessMessage"] = "Usuario desactivado correctamente";
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al desactivar usuario {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error al desactivar el usuario";
            }

            return RedirectToAction("Index");
        }
    }
}