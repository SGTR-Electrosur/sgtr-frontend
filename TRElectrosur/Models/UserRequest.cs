namespace TRElectrosur.Models
{
    public class UserCreateRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }
        public int? AreaId { get; set; }
    }

    public class UserUpdateRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleId { get; set; }
        public int? AreaId { get; set; }
        public bool IsActive { get; set; }
    }
}