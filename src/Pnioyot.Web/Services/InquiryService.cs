using Microsoft.EntityFrameworkCore;
using Pnioyot.Web.Data;
using Pnioyot.Web.Models;
using Pnioyot.Web.ViewModels;

namespace Pnioyot.Web.Services;

public class InquiryService(AppDbContext db)
{
    public IQueryable<Inquiry> Query(InquiryFilter filter)
    {
        var query = db.Inquiries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var term = filter.Q.Trim();
            query = query.Where(x =>
                x.Title.Contains(term) ||
                x.OrganizationName.Contains(term) ||
                x.AssignedTo.Contains(term));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            var assigned = filter.AssignedTo.Trim();
            query = query.Where(x => x.AssignedTo.Contains(assigned));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrganizationName))
        {
            var org = filter.OrganizationName.Trim();
            query = query.Where(x => x.OrganizationName.Contains(org));
        }

        query = filter.Sort switch
        {
            "created" => query.OrderByDescending(x => x.CreatedAt),
            "title" => query.OrderBy(x => x.Title),
            "org" => query.OrderBy(x => x.OrganizationName),
            "priority" => query.OrderByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAt),
            "status" => query.OrderBy(x => x.Status).ThenByDescending(x => x.UpdatedAt),
            _ => query.OrderByDescending(x => x.UpdatedAt)
        };

        return query;
    }

    public async Task<PagedResult<Inquiry>> SearchAsync(InquiryFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 10, 100);

        var query = Query(filter);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Inquiry>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public Task<Inquiry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Inquiries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DashboardStats> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var total = await db.Inquiries.CountAsync(cancellationToken);

        var byStatus = await db.Inquiries
            .GroupBy(x => x.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byPriority = await db.Inquiries
            .GroupBy(x => x.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var topAssignees = await db.Inquiries
            .GroupBy(x => x.AssignedTo)
            .Select(g => new AssigneeCount { AssignedTo = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var recent = await db.Inquiries.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        return new DashboardStats
        {
            Total = total,
            ByStatus = byStatus.ToDictionary(x => x.Key, x => x.Count),
            ByPriority = byPriority.ToDictionary(x => x.Key, x => x.Count),
            TopAssignees = topAssignees,
            Recent = recent
        };
    }

    public async Task<StatusUpdateResult> UpdateStatusAsync(
        Guid id,
        InquiryStatus status,
        byte[]? rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (!InquiryStatusRules.IsDefined(status))
        {
            return new StatusUpdateResult
            {
                ValidationFailed = true,
                Field = "status",
                Error = "סטטוס אינו חוקי."
            };
        }

        if (rowVersion is null || rowVersion.Length == 0)
        {
            return new StatusUpdateResult
            {
                ValidationFailed = true,
                Field = "rowVersion",
                Error = "יש לשלוח RowVersion לעדכון."
            };
        }

        var inquiry = await db.Inquiries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inquiry is null)
        {
            return new StatusUpdateResult
            {
                NotFound = true,
                Error = "הפנייה לא נמצאה."
            };
        }

        if (!inquiry.RowVersion.AsSpan().SequenceEqual(rowVersion))
        {
            return Conflict(inquiry);
        }

        if (!InquiryStatusRules.CanTransition(inquiry.Status, status))
        {
            return new StatusUpdateResult
            {
                ValidationFailed = true,
                Field = "status",
                Error = $"מעבר הסטטוס מ־{InquiryStatusRules.Label(inquiry.Status)} ל־{InquiryStatusRules.Label(status)} אינו מותר.",
                Inquiry = inquiry
            };
        }

        if (inquiry.Status == status)
        {
            return new StatusUpdateResult { Success = true, Inquiry = inquiry };
        }

        db.Entry(inquiry).Property(x => x.RowVersion).OriginalValue = rowVersion;
        inquiry.Status = status;
        inquiry.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new StatusUpdateResult { Success = true, Inquiry = inquiry };
        }
        catch (DbUpdateConcurrencyException)
        {
            await db.Entry(inquiry).ReloadAsync(cancellationToken);
            return Conflict(inquiry);
        }
    }

    public async Task<BulkStatusUpdateResult> UpdateStatusBulkAsync(
        InquiryStatus status,
        IReadOnlyList<BulkStatusItem> items,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new List<Inquiry>();
        var failed = new List<BulkStatusFailure>();
        var seen = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (!seen.Add(item.Id))
            {
                failed.Add(new BulkStatusFailure
                {
                    Id = item.Id,
                    Code = "Validation",
                    Error = "המזהה מופיע יותר מפעם אחת בבקשה."
                });
                continue;
            }

            var result = await UpdateStatusAsync(item.Id, status, item.RowVersion, cancellationToken);
            if (result.Success && result.Inquiry is not null)
            {
                succeeded.Add(result.Inquiry);
                continue;
            }

            var code = result.NotFound ? "NotFound" : result.ConcurrencyConflict ? "Conflict" : "Validation";
            failed.Add(new BulkStatusFailure
            {
                Id = item.Id,
                Code = code,
                Error = result.Error ?? "העדכון נכשל.",
                Inquiry = result.Inquiry
            });
        }

        return new BulkStatusUpdateResult
        {
            Mode = "PartialSuccess",
            RequestedCount = items.Count,
            SucceededCount = succeeded.Count,
            FailedCount = failed.Count,
            Succeeded = succeeded,
            Failed = failed
        };
    }

    private static StatusUpdateResult Conflict(Inquiry inquiry) => new()
    {
        ConcurrencyConflict = true,
        Error = "הפנייה עודכנה במקביל על ידי משתמש אחר. רעננו את המסך ונסו שוב.",
        Inquiry = inquiry
    };
}
