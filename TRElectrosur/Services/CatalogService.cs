using System.Collections.Generic;
using System.Threading.Tasks;
using TRElectrosur.Models;

namespace TRElectrosur.Services
{
    public class CatalogService
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private List<Area> _areaCache;

        public CatalogService(ApiService apiService, AuthService authService)
        {
            _apiService = apiService;
            _authService = authService;
            _areaCache = new List<Area>();
        }

        public async Task<List<Area>> GetAllAreas()
        {
            try
            {
                var token = _authService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return new List<Area>();
                }

                if (_areaCache.Count == 0)
                {
                    // Intentar cargar áreas del API
                    var areas = await _apiService.GetAsync<List<Area>>("/api/catalogs/areas", token);
                    if (areas != null && areas.Count > 0)
                    {
                        _areaCache = areas;
                        System.Console.WriteLine($"Se cargaron {areas.Count} áreas desde la API");
                    }
                    else
                    {
                        System.Console.WriteLine("No se pudieron cargar áreas desde la API, usando valores predeterminados");
                        _areaCache = new List<Area>
                        {
                            new Area { AreaID = 1, AreaName = "Logística", IsActive = true },
                            new Area { AreaID = 2, AreaName = "Tecnologías de la Información", IsActive = true },
                            new Area { AreaID = 3, AreaName = "Contabilidad", IsActive = true },
                            new Area { AreaID = 4, AreaName = "Asesoría Legal", IsActive = true },
                            new Area { AreaID = 5, AreaName = "Recursos Humanos", IsActive = true },
                            new Area { AreaID = 6, AreaName = "Gerencia General", IsActive = true }
                        };
                    }
                }

                return _areaCache;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error al obtener áreas: {ex.Message}");

                // En caso de error, devolver una lista de áreas predeterminadas
                return new List<Area>
                {
                    new Area { AreaID = 1, AreaName = "Logística", IsActive = true },
                    new Area { AreaID = 2, AreaName = "Tecnologías de la Información", IsActive = true },
                    new Area { AreaID = 3, AreaName = "Contabilidad", IsActive = true },
                    new Area { AreaID = 4, AreaName = "Asesoría Legal", IsActive = true },
                    new Area { AreaID = 5, AreaName = "Recursos Humanos", IsActive = true },
                    new Area { AreaID = 6, AreaName = "Gerencia General", IsActive = true }
                };
            }
        }

        public async Task<List<AreaSelectItem>> GetAreasForSelect(int? selectedAreaId = null)
        {
            var areas = await GetAllAreas();
            var result = new List<AreaSelectItem>();

            foreach (var area in areas)
            {
                result.Add(new AreaSelectItem
                {
                    Value = area.AreaID.ToString(),
                    Text = area.AreaName,
                    Selected = selectedAreaId.HasValue && selectedAreaId.Value == area.AreaID
                });
            }

            return result;
        }
    }
}