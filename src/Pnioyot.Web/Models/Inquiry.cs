namespace Pnioyot.Web.Models;

public enum InquiryStatus
{
    New = 0,
    InProgress = 1,
    Waiting = 2,
    Completed = 3
}

public enum InquiryPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

public class Inquiry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public InquiryStatus Status { get; set; }
    public InquiryPriority Priority { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
