using System;

namespace TRElectrosur.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleID { get; set; }
        public int? AreaID { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Propiedades computadas para la vista
        public string FullName => $"{FirstName} {LastName}";
        public string RoleName => RoleID == 1 ? "Administrador" : "Usuario";
    }
}