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
                    tdr.TDRTypeName = tipoActual?.TypeName ?? "Desconocido";
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


        // INDICACIONES PARA CADA CAMPO

        public class InstruccionCampoTDR
        {
            public string FieldName { get; set; }        // Nombre del campo
            public string Title { get; set; }            // Título de la instrucción
            public string Description { get; set; }      // Descripción detallada
            public string Example { get; set; }          // Ejemplo de llenado (opcional)
            public string Icon { get; set; }             // Icono de Font Awesome
            public bool IsRequired { get; set; }         // Si el campo es obligatorio
        }

        public static class InstruccionesTDRHelper
        {
            public static List<InstruccionCampoTDR> ObtenerInstrucciones()
            {
                // Crear la lista de instrucciones para cada campo
                return new List<InstruccionCampoTDR>
        {
            // SECCIÓN: INFORMACIÓN GENERAL
            new InstruccionCampoTDR {
                FieldName = "seccion_info_general",
                Title = "Información General",
                Description = "Esta sección contiene los datos básicos del requerimiento. Complete los campos con información clara y precisa para identificar el servicio solicitado.",
                Icon = "fas fa-info-circle",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "organoUnidadOrganica",
                Title = "Órgano y/o Unidad Orgánica",
                Description = "Indique el nombre del órgano y/o unidad orgánica responsable del requerimiento.",
                Example = "Gerencia de Administración y Finanzas - Departamento de Logística",
                Icon = "fas fa-sitemap",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "actividadPoiAccionEstrategica",
                Title = "Actividad POI / Acción Estratégica",
                Description = "Indicar el código y la denominación de la actividad del Plan Operativo Institucional o acción estratégica del PEI.",
                Example = "AO03-0001: Gestión de Recursos Humanos",
                Icon = "fas fa-clipboard-list",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "denominacionContratacion",
                Title = "Denominación de la Contratación",
                Description = "Especifique el nombre completo del servicio a contratar de manera clara y precisa.",
                Example = "Servicio de mantenimiento preventivo y correctivo del sistema eléctrico de la sede central",
                Icon = "fas fa-file-signature",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "finalidadPublica",
                Title = "Finalidad Pública",
                Description = "Describa qué se busca satisfacer, mejorar y/o atender con la contratación requerida en términos de beneficio público o institucional.",
                Example = "Garantizar la continuidad del servicio eléctrico para el desarrollo óptimo de las actividades administrativas y operativas de la institución",
                Icon = "fas fa-bullseye",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "objetivoContratacion",
                Title = "Objetivo de la Contratación",
                Description = "Especifique el objetivo principal que se pretende lograr con el servicio solicitado, detallando los beneficios esperados para la entidad.",
                Example = "Contar con un sistema eléctrico operativo y en óptimas condiciones, previniendo fallas y asegurando su funcionamiento continuo",
                Icon = "fas fa-crosshairs",
                IsRequired = true
            },

            // SECCIÓN: ALCANCES
            new InstruccionCampoTDR {
                FieldName = "seccion_alcances",
                Title = "Alcances del Servicio",
                Description = "Detalle las actividades, procedimientos, especificaciones técnicas y recursos necesarios para la correcta ejecución del servicio.",
                Icon = "fas fa-tasks",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "alcancesServicio",
                Title = "Alcances del Servicio",
                Description = "Describa detalladamente todas las actividades a desarrollar, procedimientos a seguir, plan de trabajo y recursos a ser provistos por el proveedor y por la entidad. Incluya: especificaciones técnicas, plan de trabajo, entregables esperados y recursos necesarios.",
                Icon = "fas fa-list-ul",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "reglamentos",
                Title = "Reglamentos Técnicos y Normas",
                Description = "Señale reglamentos técnicos, normas metrológicas y/o sanitarias, reglamentos y demás normas que regulan el objeto de la contratación con carácter obligatorio.",
                Example = "Ley N° 29783 - Ley de Seguridad y Salud en el Trabajo y su Reglamento, Código Nacional de Electricidad",
                Icon = "fas fa-gavel",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "seguros",
                Title = "Seguros",
                Description = "De ser el caso, precise el tipo de seguro que se exige al proveedor, la cobertura, el plazo, monto de cobertura y oportunidad de su presentación.",
                Example = "Seguro Complementario de Trabajo de Riesgo (SCTR) con cobertura de salud y pensión, con vigencia durante todo el plazo de ejecución del servicio",
                Icon = "fas fa-shield-alt",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "prestacionesAccesorias",
                Title = "Prestaciones Accesorias",
                Description = "De ser el caso, incluya las prestaciones accesorias como mantenimiento, soporte técnico, capacitación u otros servicios complementarios.",
                Example = "Capacitación al personal de la entidad en el uso y mantenimiento básico del sistema implementado",
                Icon = "fas fa-plus-circle",
                IsRequired = false
            },

            // SECCIÓN: REQUISITOS
            new InstruccionCampoTDR {
                FieldName = "seccion_requisitos",
                Title = "Requisitos del Proveedor",
                Description = "Especifique los requisitos y condiciones que debe cumplir el proveedor para la ejecución del servicio.",
                Icon = "fas fa-clipboard-check",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "requisitosProveedor",
                Title = "Requisitos del Proveedor",
                Description = "Indique los requisitos básicos que debe cumplir el proveedor, como RUC activo y habido, y Registro Nacional de Proveedores si el monto supera 1 UIT.",
                Icon = "fas fa-check-square",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "experienciaProveedor",
                Title = "Experiencia del Proveedor",
                Description = "Detalle la experiencia requerida en servicios similares, indicando el monto facturado acumulado o la cantidad de servicios ejecutados, así como el periodo de tiempo considerado (por ejemplo, últimos 8 años).",
                Example = "Monto facturado acumulado de S/ 50,000.00 en servicios similares durante los 8 años anteriores a la fecha de presentación de ofertas.",
                Icon = "fas fa-medal",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "perfilPersonal",
                Title = "Perfil del Personal",
                Description = "Describa el perfil profesional del personal clave requerido para ejecutar el servicio, incluyendo formación académica, experiencia y capacitación.",
                Icon = "fas fa-user-tie",
                IsRequired = false
            },

            // SECCIÓN: PLAZOS Y ENTREGABLES
            new InstruccionCampoTDR {
                FieldName = "seccion_plazos",
                Title = "Plazos y Entregables",
                Description = "Defina los plazos de ejecución, lugar de prestación del servicio y los entregables requeridos.",
                Icon = "fas fa-calendar-alt",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "lugar",
                Title = "Lugar de Ejecución",
                Description = "Señale el lugar donde se efectuará la prestación del servicio, especificando distrito, provincia y región.",
                Example = "Sede Central de ELECTROSUR S.A. ubicada en Calle Zela N° 408, Distrito de Tacna, Provincia de Tacna, Región Tacna",
                Icon = "fas fa-map-marker-alt",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "plazo",
                Title = "Plazo de Ejecución",
                Description = "Definir el plazo máximo en días calendario e indicar el inicio de la ejecución (a partir del día siguiente de notificado el pedido de compra u otra condición).",
                Example = "60 días calendario contados a partir del día siguiente de notificado el pedido de compra",
                Icon = "fas fa-hourglass-half",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "entregables",
                Title = "Entregables",
                Description = "Defina la forma de presentación del informe (único, mensual o por entregables) y detalle el contenido de cada entregable.",
                Example = "Primer entregable (30%): Informe de diagnóstico y plan de trabajo. Segundo entregable (70%): Informe final con resultados del servicio ejecutado.",
                Icon = "fas fa-box",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "plazoLugar",
                Title = "Plazo y Lugar de Presentación",
                Description = "Especifique el plazo máximo para la presentación de cada entregable luego de culminada su ejecución y el lugar o medio de presentación.",
                Example = "Plazo máximo de dos (02) días hábiles siguientes a la culminación de cada entregable, presentado a través de Mesa de Partes Virtual de ELECTROSUR S.A.",
                Icon = "fas fa-clipboard-list",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "conformidad",
                Title = "Conformidad",
                Description = "Indique el cargo y órgano y/o unidad orgánica responsable de otorgar la conformidad del servicio.",
                Example = "La conformidad del servicio estará a cargo del Jefe del Departamento de Logística de ELECTROSUR S.A.",
                Icon = "fas fa-thumbs-up",
                IsRequired = true
            },

            // SECCIÓN: CONDICIONES
            new InstruccionCampoTDR {
                FieldName = "seccion_condiciones",
                Title = "Condiciones",
                Description = "Especifique las condiciones de contratación, pago y otros aspectos relevantes.",
                Icon = "fas fa-file-contract",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "sistemaContratacion",
                Title = "Sistema de Contratación",
                Description = "Precise el sistema de contratación: a suma alzada o a precios unitarios.",
                Icon = "fas fa-hand-holding-usd",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "formaCondicionesPago",
                Title = "Forma y Condiciones de Pago",
                Description = "Indicar si el pago será único, mensual o por entregables, así como los requisitos para el pago.",
                Example = "Pago único, previa presentación del comprobante de pago electrónico, conformidad del servicio e informe del servicio prestado.",
                Icon = "fas fa-money-bill-wave",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "responsabilidad",
                Title = "Responsabilidad del Proveedor",
                Description = "Especifique el plazo de responsabilidad por la calidad ofrecida y los vicios ocultos del servicio, no menor a un (1) año.",
                Example = "El proveedor es responsable por la calidad ofrecida y por los vicios ocultos del servicio por un plazo de un (1) año contado a partir de la conformidad otorgada.",
                Icon = "fas fa-balance-scale",
                IsRequired = true
            },
            new InstruccionCampoTDR {
                FieldName = "otrasPenalidades",
                Title = "Otras Penalidades",
                Description = "De corresponder, establezca otras penalidades aplicables, indicando situaciones, condiciones, procedimiento de verificación y monto o porcentaje a aplicar.",
                Icon = "fas fa-exclamation-triangle",
                IsRequired = false
            },
            new InstruccionCampoTDR {
                FieldName = "medidasSeguridad",
                Title = "Medidas de Seguridad",
                Description = "Indique si corresponde alto riesgo, bajo riesgo o no corresponde, considerando la calificación otorgada por el Departamento de Seguridad y Medioambiente.",
                Icon = "fas fa-hard-hat",
                IsRequired = true
            }
        };
            }
        }

    }
}