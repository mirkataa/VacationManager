using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VacationManager.Models;

namespace VacationManager.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public new DbSet<UserModel> Users { get; set; }
        public new DbSet<RoleModel> Roles { get; set; }
        public DbSet<TeamModel> Teams { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        private bool rolesInitialized = false;

        public void Initialize()
        {
            // Check if the "Not in a team" team exists in the database
            var notInTeam = Teams.FirstOrDefault(t => t.Name == "Not in a team");
            if (notInTeam == null)
            {
                // If "Not in a team" team does not exist, create it
                notInTeam = new TeamModel
                {
                    Name = "Not in a team",
                    ProjectId = null, 
                    Developers = new List<UserModel>(), 
                    TeamLeaderId = null 
                };

                // Add the "Not in a team" team to the database
                Teams.Add(notInTeam);
                SaveChanges();
            }

            if (!rolesInitialized)
            {
                // Check if the roles exist in the database
                var ceoRole = Roles.FirstOrDefault(r => r.Name == "CEO");
                var developerRole = Roles.FirstOrDefault(r => r.Name == "Developer");
                var teamLeadRole = Roles.FirstOrDefault(r => r.Name == "Team Lead");
                var unassignedRole = Roles.FirstOrDefault(r => r.Name == "Unassigned");

                // Add roles to Role table
                if (ceoRole == null)
                {
                    // Create and add CEO role if it doesn't exist
                    ceoRole = new RoleModel { Name = "CEO", Users = new List<UserModel>() };
                    Roles.Add(ceoRole);
                }

                if (developerRole == null)
                {
                    // Create and add Developer role if it doesn't exist
                    developerRole = new RoleModel { Name = "Developer", Users = new List<UserModel>() };
                    Roles.Add(developerRole);
                }

                if (teamLeadRole == null)
                {
                    // Create and add Team Lead role if it doesn't exist
                    teamLeadRole = new RoleModel { Name = "Team Lead", Users = new List<UserModel>() };
                    Roles.Add(teamLeadRole);
                }

                if (unassignedRole == null)
                {
                    // Create and add Team Lead role if it doesn't exist
                    unassignedRole = new RoleModel { Name = "Unassigned", Users = new List<UserModel>() };
                    Roles.Add(unassignedRole);
                }

                // Save changes to the database
                SaveChanges();

                // Set the flag to true to indicate that roles have been initialized
                rolesInitialized = true;
            }
        }

        public override int SaveChanges()
        {
            var notInTeam = Teams.FirstOrDefault(t => t.Name == "Not in a team");
            if (notInTeam != null && Entry(notInTeam).State == EntityState.Deleted)
            {
                // Prevent deletion of the "Not in a team" team
                throw new InvalidOperationException("The 'Not in a team' team cannot be deleted.");
            }

            return base.SaveChanges();
        }
        public DbSet<VacationManager.Models.VacationDaysModel> VacationDaysModel { get; set; } = default!;
    }
}
