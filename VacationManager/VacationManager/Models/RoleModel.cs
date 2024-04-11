using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class RoleModel
    {
        public int Id { get; set; }

        [StringLength(20, ErrorMessage = "Name cannot be longer than 20 characters")]
        public string Name { get; set; }
        public List<UserModel> Users { get; set; }

        public RoleModel()
        {
            Users = new List<UserModel>();
        }
    }
}
