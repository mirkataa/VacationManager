namespace VacationManager.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleId { get; set; } // Foreign key for Role
        public int TeamId { get; set; } // Foreign key for Team
        public bool IsAway { get; set; }
        public bool IsHalfDay { get; set; }
        public bool IsSickLeave { get; set; }
    }
}
