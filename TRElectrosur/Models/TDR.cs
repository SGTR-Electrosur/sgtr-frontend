// Models/TDR.cs
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
        
        // Propiedades de navegación
        public string TypeName { get; set; }
        public string CurrentStateName { get; set; }
        public string CreatedByUserName { get; set; }
        public string UpdatedByUserName { get; set; }
        public string AreaName { get; set; }
    }
}