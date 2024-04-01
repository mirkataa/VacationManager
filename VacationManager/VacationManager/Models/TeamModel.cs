namespace VacationManager.Models
{
    public class TeamModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ProjectId { get; set; } // Foreign key for Project
        public List<UserModel> Developers { get; set; }
        public int? TeamLeaderId { get; set; } // Nullable foreign key for TeamLeader
        public TeamModel()
        {
            Developers = new List<UserModel>();
        }
    }
}
