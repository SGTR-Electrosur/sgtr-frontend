using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TRElectrosur.Filters;
using TRElectrosur.Models;
using TRElectrosur.Services;

namespace TRElectrosur.Controllers
{
    [Authorize]
    public class TerminosReferenciaController : Controller
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly ILogger<TerminosReferenciaController> _logger;

        public TerminosReferenciaController(ApiService apiService, AuthService authService, ILogger<TerminosReferenciaController> logger)
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
                int? roleId = _authService.GetRoleId();
                int? userId = _authService.GetUserId();

                // Si es usuario regular (roleId = 2), filtrar por su ID
                string url = "/api/tdrs";
                if (roleId.HasValue && roleId.Value == 2 && userId.HasValue)
                {
                    url += $"?userId={userId.Value}";
                }

                // Obtener los TDRs
                var tdrs = await _apiService.GetAsync<List<TDR>>(url, token);

                // Obtener los tipos de TDR para mostrarlos en el modal
                var tdrTypes = await _apiService.GetAsync<List<TDRType>>("/api/catalogs/tdrTypes", token);
                ViewBag.TDRTypes = tdrTypes ?? new List<TDRType>();
                ViewBag.Token = token;

                return View(tdrs ?? new List<TDR>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener TDRs: {ex.Message}");
                ViewBag.ErrorMessage = "No se pudieron cargar los términos de referencia";
                return View(new List<TDR>());
            }
        }

        [HttpPost]
        [Route("api/tdrs")]
        public async Task<IActionResult> CreateTDR([FromBody] TDRCreateRequest request)
        {
            try
            {
                string token = _authService.GetToken();

                var response = await _apiService.PostAsync<dynamic>("/api/tdrs", request, token);

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear TDR: {ex.Message}");
                return StatusCode(500, new { message = "Error al crear TDR: " + ex.Message });
            }
        }

        public async Task<IActionResult> Editar(int id, int? versionId = null)
        {
            try
            {
                string token = _authService.GetToken();

                // Obtener los detalles del TDR
                var tdr = await _apiService.GetAsync<TDR>($"/api/tdrs/{id}", token);
                if (tdr == null)
                {
                    TempData["ErrorMessage"] = "TDR no encontrado";
                    return RedirectToAction("Index");
                }

                // Obtener información adicional si es necesario
                var tdrEstates = await _apiService.GetAsync<List<TDRState>>("/api/catalogs/tdrEstates", token);
                var tdrTypes = await _apiService.GetAsync<List<TDRType>>("/api/catalogs/tdrTypes", token);

                // Asignar los nombres a partir de los catálogos
                tdr.CurrentStateName = tdrEstates?.FirstOrDefault(e => e.StateID == tdr.CurrentStateID)?.StateName ?? "Desconocido";
                tdr.TDRTypeName = tdrTypes?.FirstOrDefault(t => t.TDRTypeID == tdr.TDRTypeID)?.TypeName ?? "Desconocido";

                // Obtener todas las versiones de este TDR
                var versiones = await _apiService.GetAsync<List<TDRVersion>>($"/api/tdrs/{id}/versions", token);
                ViewBag.Versiones = versiones ?? new List<TDRVersion>();

                // Determinar la versión actual a mostrar
                int currentVersionId = versionId ?? tdr.CurrentVersionID ?? 0;
                ViewBag.VersionActual = currentVersionId;

                return View(tdr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener TDR para editar: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el TDR";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ObtenerCampos(int id, int versionId)
        {
            try
            {
                string token = _authService.GetToken();

                // Obtener los campos de la versión específica
                var fields = await _apiService.GetAsync<List<TDRVersionField>>($"/api/tdrs/{id}/versions/{versionId}/fields", token);

                return Json(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener campos: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener campos: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(int id, int versionId, Dictionary<string, string> fields)
        {
            try
            {
                string token = _authService.GetToken();
                bool success = true;
                List<string> errors = new List<string>();

                // Guardar cada campo modificado
                foreach (var field in fields)
                {
                    string fieldName = field.Key;
                    string htmlContent = field.Value;

                    // Obtener el campo para encontrar su ID
                    var fieldsResponse = await _apiService.GetAsync<List<TDRVersionField>>($"/api/tdrs/{id}/versions/{versionId}/fields", token);
                    var fieldToUpdate = fieldsResponse?.FirstOrDefault(f => f.FieldName == fieldName);

                    if (fieldToUpdate != null)
                    {
                        // Preparar el objeto de actualización
                        var updateRequest = new { htmlContent = htmlContent };

                        // Actualizar el campo
                        var response = await _apiService.PutAsync<dynamic>(
                            $"/api/tdrs/{id}/versions/{versionId}/fields/{fieldToUpdate.FieldID}",
                            updateRequest,
                            token);

                        if (response == null)
                        {
                            success = false;
                            errors.Add($"Error al actualizar el campo {fieldName}");
                        }
                    }
                }

                if (success)
                {
                    TempData["SuccessMessage"] = "Cambios guardados correctamente";
                    return RedirectToAction("Editar", new { id = id, versionId = versionId });
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join(". ", errors);
                    return RedirectToAction("Editar", new { id = id, versionId = versionId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar cambios: {ex.Message}");
                TempData["ErrorMessage"] = "Error al guardar los cambios";
                return RedirectToAction("Editar", new { id = id, versionId = versionId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, int newStateId, string reason)
        {
            try
            {
                string token = _authService.GetToken();

                // Preparar la solicitud
                var request = new
                {
                    newStateId = newStateId,
                    reason = reason
                };

                // Enviar la solicitud para cambiar el estado
                var response = await _apiService.PutAsync<dynamic>($"/api/tdrs/{id}/state", request, token);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "Estado actualizado correctamente";
                    return Json(new { success = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Error al cambiar el estado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al cambiar estado: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error al cambiar el estado: " + ex.Message });
            }
        }

        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                string token = _authService.GetToken();

                // Realizar la solicitud al endpoint de descarga (asumiendo que existe o tendría que implementarse)
                var response = await _apiService.GetAsync<byte[]>($"/api/tdrs/{id}/pdf", token);

                if (response != null)
                {
                    // Devolver el archivo PDF
                    return File(response, "application/pdf", $"TDR-{id}.pdf");
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo generar el PDF";
                    return RedirectToAction("Editar", new { id = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al descargar PDF: {ex.Message}");
                TempData["ErrorMessage"] = "Error al generar el PDF";
                return RedirectToAction("Editar", new { id = id });
            }
        }

        public IActionResult Crear(int tipo)
        {
            ViewBag.TipoTDR = tipo;
            return View();
        }
    }
}