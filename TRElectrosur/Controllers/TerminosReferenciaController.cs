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

        public async Task<IActionResult> Editar(int id)
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

                // Obtener información adicional
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
                    tdr.TypeName = tipoActual?.TypeName ?? "Desconocido";
                }

                // Obtener todas las versiones de este TDR
                var versiones = await _apiService.GetAsync<List<TDRVersion>>($"/api/tdrs/{id}/versions", token);

                // Obtener los campos directamente (sin intentar deserializar a tipo dinámico)
                var fieldsResponseStr = await _apiService.GetStringAsync($"/api/tdrs/{id}/fields", token);

                // Crear el ViewModel
                var viewModel = new TDREditViewModel
                {
                    TDR = tdr,
                    Versiones = versiones ?? new List<TDRVersion>(),
                    VersionActual = tdr.CurrentVersionID ?? 0,
                    IsAdmin = userRoleId == 1,
                    CanEdit = false,
                    UserRoleId = userRoleId,
                    RawFieldsData = fieldsResponseStr // Guardar los datos crudos como string
                };

                // Determinar qué vista mostrar según el tipo de TDR
                if (tdr.TDRTypeID == 3) // Tipo 3 corresponde a ETT
                {
                    return View("EditarETT", viewModel);
                }
                else
                {
                    return View(viewModel); // Vista Editar por defecto
                }
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
        public async Task<IActionResult> Guardar(int id, Dictionary<string, string> fields, Dictionary<string, string> observations)
        {
            try
            {
                string token = _authService.GetToken();
                int? userRoleId = _authService.GetRoleId();
                bool isAdmin = userRoleId == 1;

                _logger.LogInformation($"Guardando TDR ID: {id}, Campos: {fields.Count}, Observaciones: {observations?.Count ?? 0}");

                // Primero obtenemos la información del TDR
                var fieldsData = await _apiService.GetStringAsync($"/api/tdrs/{id}/fields", token);
                if (string.IsNullOrEmpty(fieldsData))
                {
                    TempData["ErrorMessage"] = "No se pudo obtener la información actual del TDR";
                    return RedirectToAction("Editar", new { id = id });
                }

                // Parseamos el JSON para obtener los fieldIDs
                var fieldsDoc = System.Text.Json.JsonDocument.Parse(fieldsData);
                var fieldsArray = fieldsDoc.RootElement.GetProperty("fields");

                // Preparamos la lista de campos a actualizar
                var updateList = new List<Dictionary<string, object>>();

                foreach (var fieldElement in fieldsArray.EnumerateArray())
                {
                    string fieldName = fieldElement.GetProperty("FieldName").GetString();
                    int fieldId = fieldElement.GetProperty("FieldID").GetInt32();

                    // Si el campo está en los datos enviados por el formulario
                    if (fields.ContainsKey(fieldName))
                    {
                        var fieldData = new Dictionary<string, object>
                {
                    { "fieldId", fieldId },
                    { "htmlContent", fields[fieldName] }
                };

                        // Si hay observaciones y el usuario es admin
                        if (isAdmin && observations != null && observations.ContainsKey(fieldName))
                        {
                            fieldData.Add("comment", observations[fieldName]);
                        }

                        updateList.Add(fieldData);
                    }
                }

                // Enviamos la lista al API
                var response = await _apiService.PutAsync<dynamic>($"/api/tdrs/{id}/fields", updateList, token);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "Información actualizada correctamente";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo actualizar la información del TDR";
                    return RedirectToAction("Editar", new { id = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar TDR: {ex.Message}");
                TempData["ErrorMessage"] = "Error al guardar los cambios: " + ex.Message;
                return RedirectToAction("Editar", new { id = id });
            }
        }


        // Agregar estos métodos a la clase TerminosReferenciaController
        [HttpPost]
        public async Task<JsonResult> CambiarEstado(int id, int newStateId, string reason)
        {
            try
            {
                string token = _authService.GetToken();

                var request = new
                {
                    newStateId = newStateId,
                    reason = reason
                };

                await _apiService.PutAsync<object>($"/api/tdrs/{id}/state", request, token);

                // Enviar URL para recargar la página de edición con el ID correspondiente
                var redirectUrl = Url.Action("Editar", "TerminosReferencia", new { id = id });
                return Json(new { success = true, redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al cambiar estado: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CrearNuevaVersion(int id)
        {
            try
            {
                string token = _authService.GetToken();

                // API espera una solicitud POST sin cuerpo
                var response = await _apiService.PostAsync<dynamic>($"/api/tdrs/{id}/versions", null, token);

                if (response != null)
                {
                    TempData["SuccessMessage"] = "Nueva versión creada correctamente";
                    return Json(new { success = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Error al crear nueva versión" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear nueva versión: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error al crear nueva versión: " + ex.Message });
            }
        }


        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                string token = _authService.GetToken();

                // Utilizar directamente el ApiService para mantener consistencia con el resto de la aplicación
                // Este servicio ya tiene configurada la URL base desde appsettings.json
                var response = await _apiService.GetFileAsync($"/api/tdrs/{id}/export-pdf", token);

                if (response != null && response.FileContent != null)
                {
                    // Devolver el archivo PDF
                    return File(response.FileContent, "application/pdf", response.Filename ?? $"TDR-{id}.pdf");
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
                TempData["ErrorMessage"] = "Error al generar el PDF: " + ex.Message;
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