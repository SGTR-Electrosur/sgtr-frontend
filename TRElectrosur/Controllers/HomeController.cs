using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TRElectrosur.Models;
using TRElectrosur.Services;

namespace TRElectrosur.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuthService _authService;

        public HomeController(ILogger<HomeController> logger, AuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public IActionResult Index()
        {
            // Si ya está autenticado, redirigir a TerminosReferencia
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Index", "TerminosReferencia");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "El correo y la contraseña son obligatorios";
                return View("Index");
            }

            _logger.LogInformation($"Intentando login con email: {email}");

            var (response, errorMessage) = await _authService.Login(email, password);

            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogInformation("Login exitoso");
                return RedirectToAction("Index", "TerminosReferencia");
            }

            ViewBag.ErrorMessage = errorMessage ?? "Credenciales incorrectas";
            _logger.LogWarning($"Error de login: {ViewBag.ErrorMessage}");
            return View("Index");
        }

        public IActionResult Logout()
        {
            // Limpiar la sesión
            HttpContext.Session.Clear();

            // Redirigir al login
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}