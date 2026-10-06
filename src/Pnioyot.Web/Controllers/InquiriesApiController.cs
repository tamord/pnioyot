using System.Globalization;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pnioyot.Web.Models;
using Pnioyot.Web.Services;
using Pnioyot.Web.ViewModels;

namespace Pnioyot.Web.Controllers;

[ApiController]
[Route("api/inquiries")]
[Produces("application/json")]
public class InquiriesApiController(InquiryService inquiries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Inquiry>), StatusCodes.Status200OK)]
    [Produces("application/json", "text/csv")]
    public async Task<IActionResult> List([FromQuery] InquiryFilter filter, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return await ExportCsvAsync(filter, cancellationToken);
        }

        return Ok(await inquiries.SearchAsync(filter, cancellationToken));
    }

    [HttpGet("stats")]
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var stats = await inquiries.GetDashboardAsync(cancellationToken);
        return Ok(new
        {
            stats.Total,
            ByStatus = stats.ByStatus.ToDictionary(x => x.Key.ToString(), x => x.Value),
            ByPriority = stats.ByPriority.ToDictionary(x => x.Key.ToString(), x => x.Value),
            stats.TopAssignees,
            stats.Recent
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Inquiry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Inquiry>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await inquiries.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    public class StatusPatch
    {
        [Required]
        public InquiryStatus? Status { get; set; }

        [Required]
        public byte[]? RowVersion { get; set; }
    }

    [HttpPatch("{id:guid}")]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(Inquiry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] StatusPatch body, CancellationToken cancellationToken)
    {
        var result = await inquiries.UpdateStatusAsync(id, body.Status!.Value, body.RowVersion, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(result);
        }

        if (result.ValidationFailed)
        {
            ModelState.AddModelError(result.Field ?? "status", result.Error ?? "בקשה לא תקינה.");
            return ValidationProblem(ModelState);
        }

        if (result.ConcurrencyConflict)
        {
            return Conflict(result);
        }

        return Ok(result.Inquiry);
    }

    /// <summary>
    /// REST collection PATCH. Partial Success: each inquiry is updated independently
    /// (up to 100). Missing rows, illegal transitions, and RowVersion conflicts do not
    /// roll back successful updates.
    /// HTTP 200 = all succeeded, 207 = at least one failure, 400 = invalid request.
    /// </summary>
    [HttpPatch]
    [HttpPost("bulk-status")]
    [ProducesResponseType(typeof(BulkStatusUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BulkStatusUpdateResult), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchCollection([FromBody] BulkStatusRequest body, CancellationToken cancellationToken)
    {
        if (body.Items.Count is < 1 or > 100)
        {
            ModelState.AddModelError("items", "יש לשלוח בין פנייה אחת ל־100 פניות.");
            return ValidationProblem(ModelState);
        }

        var result = await inquiries.UpdateStatusBulkAsync(body.Status!.Value, body.Items, cancellationToken);
        if (result.FailedCount == 0)
        {
            return Ok(result);
        }

        return StatusCode(StatusCodes.Status207MultiStatus, result);
    }

    [HttpGet("export")]
    [Produces("text/csv")]
    public Task<IActionResult> Export([FromQuery] InquiryFilter filter, CancellationToken cancellationToken)
        => ExportCsvAsync(filter, cancellationToken);

    private async Task<IActionResult> ExportCsvAsync(InquiryFilter filter, CancellationToken cancellationToken)
    {
        var query = inquiries.Query(filter);
        var items = await query.ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Id,Title,OrganizationName,Status,Priority,AssignedTo,CreatedAt,UpdatedAt");
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(',',
                Csv(item.Id.ToString()),
                Csv(item.Title),
                Csv(item.OrganizationName),
                Csv(item.Status.ToString()),
                Csv(item.Priority.ToString()),
                Csv(item.AssignedTo),
                Csv(item.CreatedAt.ToString("o", CultureInfo.InvariantCulture)),
                Csv(item.UpdatedAt.ToString("o", CultureInfo.InvariantCulture))));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var name = $"inquiries-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", name);
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
