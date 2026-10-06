using System.ComponentModel.DataAnnotations;
using Pnioyot.Web.Models;

namespace Pnioyot.Web.ViewModels;

public class InquiryFilter
{
    public string? Q { get; set; }
    public InquiryStatus? Status { get; set; }
    public InquiryPriority? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public string? OrganizationName { get; set; }
    public string Sort { get; set; } = "updated";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class DashboardStats
{
    public int Total { get; init; }
    public IReadOnlyDictionary<InquiryStatus, int> ByStatus { get; init; } = new Dictionary<InquiryStatus, int>();
    public IReadOnlyDictionary<InquiryPriority, int> ByPriority { get; init; } = new Dictionary<InquiryPriority, int>();
    public IReadOnlyList<AssigneeCount> TopAssignees { get; init; } = [];
    public IReadOnlyList<Inquiry> Recent { get; init; } = [];
}

public class AssigneeCount
{
    public required string AssignedTo { get; init; }
    public int Count { get; init; }
}

public class StatusUpdateResult
{
    public bool Success { get; init; }
    public bool NotFound { get; init; }
    public bool ValidationFailed { get; init; }
    public bool ConcurrencyConflict { get; init; }
    public string? Error { get; init; }
    public string? Field { get; init; }
    public Inquiry? Inquiry { get; init; }
}

public class BulkStatusItem
{
    public Guid Id { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class BulkStatusRequest
{
    [Required]
    public InquiryStatus? Status { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<BulkStatusItem> Items { get; set; } = [];
}

public class BulkStatusFailure
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Error { get; init; }
    public Inquiry? Inquiry { get; init; }
}

public class BulkStatusUpdateResult
{
    public string Mode { get; init; } = "PartialSuccess";
    public int RequestedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<Inquiry> Succeeded { get; init; } = [];
    public IReadOnlyList<BulkStatusFailure> Failed { get; init; } = [];
}
