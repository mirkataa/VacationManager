using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class Project
    {
        public int Id { get; set; }

        [StringLength(20, ErrorMessage = "Name cannot be longer than 20 characters")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Description cannot be longer than 100 characters")]
        public string Description { get; set; }
        public List<TeamModel> Teams { get; set; }
        public Project()
        {
            Teams = new List<TeamModel>();
        }
    }
}
