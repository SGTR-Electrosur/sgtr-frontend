namespace TRElectrosur.Models
{
    public class TDRVersion
    {
        public int TDRVersionID { get; set; }
        public int TDRID { get; set; }
        public int VersionNumber { get; set; }
        public int StateID { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByUserID { get; set; }
        public string CreatedByUserName { get; set; }
    }
}