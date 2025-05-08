using Microsoft.AspNetCore.Http;
using TRElectrosur.Models;

namespace TRElectrosur.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(ApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(LoginResponse Response, string ErrorMessage)> Login(string email, string password)
        {
            Console.WriteLine($"Intentando login con email: {email}");

            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            try
            {
                var response = await _apiService.PostAsync<LoginResponse>("/api/auth/login", loginRequest);

                if (response != null && !string.IsNullOrEmpty(response.Token))
                {
                    // Guardar el token en la sesión
                    _httpContextAccessor.HttpContext.Session.SetString("AuthToken", response.Token);

                    // Guardar datos del usuario en la sesión
                    _httpContextAccessor.HttpContext.Session.SetString("UserName", $"{response.User.FirstName} {response.User.LastName}");
                    _httpContextAccessor.HttpContext.Session.SetInt32("RoleId", response.User.RoleId);
                    _httpContextAccessor.HttpContext.Session.SetInt32("UserId", response.User.Id);

                    if (response.User.AreaId.HasValue)
                    {
                        _httpContextAccessor.HttpContext.Session.SetInt32("AreaId", response.User.AreaId.Value);
                    }

                    return (response, null);
                }
                else
                {
                    return (null, "Credenciales incorrectas.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante el login: {ex.Message}");
                return (null, $"Error de conexión: {ex.Message}");
            }
        }

        public string GetToken()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("AuthToken");
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetToken());
        }

        public string GetUserName()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("UserName");
        }

        public int? GetRoleId()
        {
            return _httpContextAccessor.HttpContext.Session.GetInt32("RoleId");
        }

        public int? GetAreaId()
        {
            return _httpContextAccessor.HttpContext.Session.GetInt32("AreaId");
        }

        public int? GetUserId()
        {
            return _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
        }
    }
}