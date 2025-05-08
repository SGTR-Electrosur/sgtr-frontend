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
        public FieldReview lastReview { get; set; }
    }
}