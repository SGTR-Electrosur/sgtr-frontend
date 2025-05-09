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
                int? userRoleId = _authService.GetRoleId();

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
                if (tdrEstates != null)
                {
                    var estadoActual = tdrEstates.FirstOrDefault(e => e.StateID == tdr.CurrentStateID);
                    tdr.CurrentStateName = estadoActual?.StateName ?? "Desconocido";
                }

                if (tdrTypes != null)
                {
                    var tipoActual = tdrTypes.FirstOrDefault(t => t.TDRTypeID == tdr.TDRTypeID);
                    tdr.TDRTypeName = tipoActual?.TypeName ?? "Desconocido";
                }

                // Obtener todas las versiones de este TDR
                var versiones = await _apiService.GetAsync<List<TDRVersion>>($"/api/tdrs/{id}/versions", token);

                // Determinar la versión actual a mostrar
                int currentVersionId = versionId ?? tdr.CurrentVersionID ?? 0;

                // Determinar si el usuario puede editar según el estado y rol
                bool canEdit = false;
                if (userRoleId == 1) // Admin puede editar en cualquier estado
                {
                    canEdit = true;
                }
                else // Usuario normal 
                {
                    // Solo puede editar en estado Borrador (1) o Observado (3)
                    canEdit = tdr.CurrentStateID == 1 || tdr.CurrentStateID == 3;
                }

                // Crear el ViewModel
                var viewModel = new TDREditViewModel
                {
                    TDR = tdr,
                    Versiones = versiones ?? new List<TDRVersion>(),
                    VersionActual = currentVersionId,
                    IsAdmin = userRoleId == 1, // 1 = admin, 2 = usuario
                    CanEdit = canEdit,
                    UserRoleId = userRoleId
                };

                return View(viewModel);
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

                _logger.LogInformation($"Obteniendo campos para TDR ID: {id}, Versión: {versionId}");

                // Obtener los campos de la versión específica
                var fields = await _apiService.GetAsync<List<TDRVersionField>>($"/api/tdrs/{id}/versions/{versionId}/fields", token);

                if (fields == null)
                {
                    _logger.LogWarning($"No se encontraron campos para el TDR {id}, versión {versionId}");
                    return Json(new List<TDRVersionField>());
                }

                _logger.LogInformation($"Se obtuvieron {fields.Count} campos para el TDR {id}");

                return Json(fields);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener campos: {ex.Message}");
                return StatusCode(500, new { message = "Error al obtener campos: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(int id, int versionId, Dictionary<string, string> fields, Dictionary<string, string> observations = null)
        {
            try
            {
                string token = _authService.GetToken();
                int? userRoleId = _authService.GetRoleId();
                bool success = true;
                List<string> errors = new List<string>();

                _logger.LogInformation($"Guardando TDR ID: {id}, Versión: {versionId}, Campos: {fields.Count}, Observaciones: {observations?.Count ?? 0}");

                // Verificar que el usuario tiene permisos para guardar
                var tdr = await _apiService.GetAsync<TDR>($"/api/tdrs/{id}", token);
                if (tdr == null)
                {
                    TempData["ErrorMessage"] = "TDR no encontrado";
                    return RedirectToAction("Editar", new { id = id, versionId = versionId });
                }

                bool canEdit = false;
                if (userRoleId == 1) // Admin puede editar en cualquier estado
                {
                    canEdit = true;
                }
                else // Usuario normal 
                {
                    // Solo puede editar en estado Borrador (1) o Observado (3)
                    canEdit = tdr.CurrentStateID == 1 || tdr.CurrentStateID == 3;
                }

                if (!canEdit)
                {
                    TempData["ErrorMessage"] = "No tiene permisos para modificar este TDR en su estado actual";
                    return RedirectToAction("Editar", new { id = id, versionId = versionId });
                }

                // Obtener primero los campos actuales para tener los IDs correctos
                var fieldsResponse = await _apiService.GetAsync<List<TDRVersionField>>($"/api/tdrs/{id}/versions/{versionId}/fields", token);

                if (fieldsResponse == null)
                {
                    _logger.LogError($"No se pudo obtener la lista de campos para el TDR {id}, versión {versionId}");
                    TempData["ErrorMessage"] = "Error al obtener los campos del TDR";
                    return RedirectToAction("Editar", new { id = id, versionId = versionId });
                }

                // Guardar cada campo modificado
                foreach (var field in fields)
                {
                    string fieldName = field.Key;
                    string htmlContent = field.Value;

                    // Buscar el campo por nombre
                    var fieldToUpdate = fieldsResponse.FirstOrDefault(f => f.FieldName == fieldName);

                    if (fieldToUpdate != null)
                    {
                        _logger.LogInformation($"Actualizando campo {fieldName} (ID: {fieldToUpdate.FieldID})");

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
                            _logger.LogError($"Error al actualizar el campo {fieldName} del TDR {id}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No se encontró el campo {fieldName} en la lista de campos del TDR {id}, versión {versionId}");
                    }
                }

                // Si es admin y hay observaciones, guardarlas
                if (userRoleId == 1 && observations != null && observations.Count > 0)
                {
                    _logger.LogInformation($"Guardando {observations.Count} observaciones como administrador");

                    foreach (var obs in observations)
                    {
                        string fieldName = obs.Key;
                        string comment = obs.Value;

                        if (string.IsNullOrWhiteSpace(comment))
                        {
                            continue; // No guardar observaciones vacías
                        }

                        // Buscar el campo correspondiente
                        var fieldToUpdate = fieldsResponse.FirstOrDefault(f => f.FieldName == fieldName);

                        if (fieldToUpdate != null && fieldToUpdate.VersionFieldID > 0)
                        {
                            _logger.LogInformation($"Guardando observación para campo {fieldName} (ID: {fieldToUpdate.FieldID})");

                            // Las observaciones se guardarían mediante una llamada a la API de revisión
                            // Por ahora solo lo registramos en el log ya que no vamos a implementar esta funcionalidad todavía
                            _logger.LogInformation($"Contenido de la observación: {comment}");

                            // TODO: Implementar cuando se active la funcionalidad de observaciones
                            // var reviewRequest = new { reviewStatus = "observado", comment = comment };
                            // var response = await _apiService.PostAsync<dynamic>(
                            //    $"/api/tdrs/{id}/versions/{versionId}/fields/{fieldToUpdate.FieldID}/review",
                            //    reviewRequest,
                            //    token);
                        }
                    }
                }

                if (success)
                {
                    TempData["SuccessMessage"] = "Cambios guardados correctamente";
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join(". ", errors);
                }

                return RedirectToAction("Editar", new { id = id, versionId = versionId });
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

        [HttpPost]
        public async Task<IActionResult> GuardarCampo([FromBody] TDRCampoUpdateRequest request)
        {
            try
            {
                string token = _authService.GetToken();

                _logger.LogInformation($"Guardando campo - TDR: {request.tdrId}, Versión: {request.versionId}, FieldID: {request.fieldId}");

                // Preparar el objeto de actualización
                var updateRequest = new { htmlContent = request.htmlContent ?? "" };

                // Ejecutar la actualización
                var response = await _apiService.PutAsync<dynamic>(
                    $"/api/tdrs/{request.tdrId}/versions/{request.versionId}/fields/{request.fieldId}",
                    updateRequest,
                    token);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar campo: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class TDRCampoUpdateRequest
        {
            public int tdrId { get; set; }
            public int versionId { get; set; }
            public int fieldId { get; set; }
            public string htmlContent { get; set; }
        }
    }
}