// Models/TDRFieldsResponse.cs
namespace TRElectrosur.Models
{
    public class TDRFieldsResponse
    {
        public TDRVersion Version { get; set; }
        public List<TDRVersionField> Fields { get; set; }
    }

    public class TDRFieldReview
    {
        public int? ReviewID { get; set; }
        public int VersionFieldID { get; set; }
        public int? ReviewedByUserID { get; set; }
        public string ReviewedByUserName { get; set; }
        public string ReviewStatus { get; set; }
        public string Comment { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}