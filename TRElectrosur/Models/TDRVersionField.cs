namespace TRElectrosur.Models
{
    public class TDRVersionField
    {
        public int VersionFieldID { get; set; }
        public int TDRVersionID { get; set; }
        public int FieldID { get; set; }
        public string FieldName { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public int FieldOrder { get; set; }
        public string HtmlContent { get; set; }
        public string comment { get; set; }  // Para almacenar comentarios/observaciones
        public FieldReview lastReview { get; set; }  // Última revisión del campo
    }

    public class FieldReview
    {
        public int? ReviewID { get; set; }
        public int VersionFieldID { get; set; }
        public int? ReviewedByUserID { get; set; }
        public string ReviewedByUserName { get; set; }
        public string ReviewStatus { get; set; }  // "aprobado" o "observado"
        public string Comment { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}