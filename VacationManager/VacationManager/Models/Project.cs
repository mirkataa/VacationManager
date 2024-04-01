namespace VacationManager.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TeamModel> Teams { get; set; }
        public Project()
        {
            Teams = new List<TeamModel>();
        }
    }
}
