using Microsoft.AspNetCore.Mvc;
using TRElectrosur.Filters;
using TRElectrosur.Services;

namespace TRElectrosur.Controllers
{
    [Authorize]
    public class TerminosReferenciaController : Controller
    {
        private readonly AuthService _authService;

        public TerminosReferenciaController(AuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Index()
        {
            ViewBag.UserName = _authService.GetUserName();
            ViewBag.RoleId = _authService.GetRoleId();
            ViewBag.AreaId = _authService.GetAreaId();

            return View();
        }

        public IActionResult Crear()
        {
            return View();
        }
    }
}