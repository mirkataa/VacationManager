namespace VacationManager.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RequestCreationDate { get; set; }
        public bool IsHalfDay { get; set; }
        public bool IsApproved { get; set; }
        public int ApplicantId { get; set; } // Foreign key for User
        public bool IsSickLeave { get; set; }
        public byte[]? MedicalCertificate { get; set; } // File content
        public int? ApproverId { get; set; } // Foreign key for User, nullable
    }
}
