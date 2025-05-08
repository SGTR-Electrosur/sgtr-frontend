namespace TRElectrosur.Models
{
    public class FieldReview
    {
        public int ReviewID { get; set; }
        public int VersionFieldID { get; set; }
        public int ReviewedByUserID { get; set; }
        public string ReviewedByUserName { get; set; }
        public string ReviewStatus { get; set; } // "aprobado" o "observado"
        public string Comment { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}