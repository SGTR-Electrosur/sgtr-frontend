namespace TRElectrosur.Models
{
    public class TDRCreateRequest
    {
        public string Title { get; set; }
        public int TdrTypeId { get; set; }
    }

    public class TDRCampoUpdateRequest
    {
        public int tdrId { get; set; }
        public int versionId { get; set; }
        public int fieldId { get; set; }
        public string htmlContent { get; set; }
    }
}
