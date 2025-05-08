namespace TRElectrosur.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public UserData User { get; set; }
        public string Token { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleId { get; set; }
        public int? AreaId { get; set; }
    }
}