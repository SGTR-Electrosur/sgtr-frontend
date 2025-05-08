using System.Collections.Generic;
using System.Threading.Tasks;
using TRElectrosur.Models;
using TRElectrosur.Services;

namespace TRElectrosur.Services
{
    public class UserService
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly CatalogService _catalogService;

        public UserService(ApiService apiService, AuthService authService, CatalogService catalogService)
        {
            _apiService = apiService;
            _authService = authService;
            _catalogService = catalogService;
        }

        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                // Obtener el token de autenticación
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return new List<User>();
                }

                // Realizar la petición a la API
                var users = await _apiService.GetAsync<List<User>>("/api/users", token);
                return users ?? new List<User>();
            }
            catch (System.Exception ex)
            {
                // Registrar el error y devolver una lista vacía
                System.Console.WriteLine($"Error al obtener usuarios: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<User> GetUserById(int id)
        {
            try
            {
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                return await _apiService.GetAsync<User>($"/api/users/{id}", token);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al obtener usuario {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateUser(UserCreateRequest request)
        {
            try
            {
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _apiService.PostAsync<dynamic>("/api/users", request, token);
                return response != null;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al crear usuario: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUser(int id, UserUpdateRequest request)
        {
            try
            {
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _apiService.PutAsync<dynamic>($"/api/users/{id}", request, token);
                return response != null;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al actualizar usuario {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserPassword(int id, string newPassword)
        {
            try
            {
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var request = new { newPassword = newPassword };
                var response = await _apiService.PutAsync<dynamic>($"/api/users/{id}/password", request, token);
                return response != null;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al cambiar contraseña de usuario {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeactivateUser(int id)
        {
            try
            {
                var token = _authService.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _apiService.PutAsync<dynamic>($"/api/users/{id}/deactivate", null, token);
                return response != null;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al desactivar usuario {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetAreaName(int? areaId)
        {
            if (!areaId.HasValue)
                return "No asignada";

            var areas = await _catalogService.GetAllAreas();
            var area = areas.Find(a => a.AreaID == areaId.Value);
            return area != null ? area.AreaName : $"Área {areaId}";
        }

        public async Task<List<AreaSelectItem>> GetAreasForSelect(int? selectedAreaId = null)
        {
            return await _catalogService.GetAreasForSelect(selectedAreaId);
        }

        public List<RoleSelectItem> GetRolesForSelect(int? selectedRoleId = null)
        {
            var result = new List<RoleSelectItem>
            {
                new RoleSelectItem
                {
                    Value = "1",
                    Text = "Administrador",
                    Selected = selectedRoleId.HasValue && selectedRoleId.Value == 1
                },
                new RoleSelectItem
                {
                    Value = "2",
                    Text = "Usuario",
                    Selected = selectedRoleId.HasValue && selectedRoleId.Value == 2
                }
            };

            return result;
        }
    }
}