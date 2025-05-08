// Models/TDR.cs
using System.Text.Json.Serialization;

namespace TRElectrosur.Models
{
    public class TDR
    {
        public int TDRID { get; set; }
        public string Title { get; set; }
        public int CreatedByUserID { get; set; }
        public int TDRTypeID { get; set; }
        public int CurrentStateID { get; set; }
        public int? CurrentVersionID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByUserID { get; set; }

        // Propiedades para la vista (elegir una de las dos implementaciones)

        // Si la API devuelve TypeName y StateName
        public string TypeName { get; set; }
        public string StateName { get; set; }

        // O usar ambos nombres de propiedades con JsonPropertyName si es necesario
        [JsonPropertyName("typeName")]
        public string TDRTypeName { get; set; }

        [JsonPropertyName("stateName")]
        public string CurrentStateName { get; set; }

        public string CreatedByUserName { get; set; }
        public string UpdatedByUserName { get; set; }
        public string AreaName { get; set; }
    }
}