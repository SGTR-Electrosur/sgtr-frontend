using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRElectrosur.Filters;
using TRElectrosur.Models;
using TRElectrosur.Services;

namespace TRElectrosur.Controllers
{
    [Authorize]
    public class DebugAreasController : Controller
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly ILogger<DebugAreasController> _logger;

        public DebugAreasController(ApiService apiService, AuthService authService, ILogger<DebugAreasController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.ErrorMessage = "No se ha iniciado sesión";
                return View();
            }

            try
            {
                var areas = await _apiService.GetAsync<object>("/api/catalogs/areas", token);
                ViewBag.AreasJson = System.Text.Json.JsonSerializer.Serialize(areas, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return View();
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al obtener áreas: {ex.Message}");
                ViewBag.ErrorMessage = $"Error al obtener áreas: {ex.Message}";
                return View();
            }
        }
    }
}