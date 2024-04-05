using System.ComponentModel.DataAnnotations;

namespace VacationManager.Models
{
    public class VacationDaysModel
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Foreign key for User
        public int Year { get; set; }

        [Range(20, 55, ErrorMessage = "Vacation days must be between 20 and 55.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Vacation days must be a whole number.")]
        public double VacationDays { get; set; }

        [Range(0, 55, ErrorMessage = "Used days must be between 0 and 55.")]
        [RegularExpression(@"^\d+(\.5)?$", ErrorMessage = "Used days must be a whole number or end with .5.")]
        public double UsedDays { get; set; }
        public double PendingDays { get; set; }
    }
}
