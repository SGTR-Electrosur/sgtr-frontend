// Models/TDREditViewModel.cs
namespace TRElectrosur.Models
{
    public class TDREditViewModel
    {
        public TDR TDR { get; set; }
        public List<TDRVersion> Versiones { get; set; }
        public int VersionActual { get; set; }
        public bool IsAdmin { get; set; }
        public bool CanEdit { get; set; }
        public int? UserRoleId { get; set; }
        public string RawFieldsData { get; set; }
    }
}