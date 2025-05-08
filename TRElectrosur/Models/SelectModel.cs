namespace TRElectrosur.Models
{
    public class SelectItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
    }

    public class RoleSelectItem : SelectItem
    {
    }

    public class AreaSelectItem : SelectItem
    {
    }
}