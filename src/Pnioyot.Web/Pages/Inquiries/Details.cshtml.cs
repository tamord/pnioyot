using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pnioyot.Web.Models;
using Pnioyot.Web.Services;

namespace Pnioyot.Web.Pages.Inquiries;

public class DetailsModel(InquiryService inquiries) : PageModel
{
    [BindProperty]
    public Inquiry Inquiry { get; set; } = new();

    [BindProperty]
    public InquiryStatus NewStatus { get; set; }

    public IReadOnlyList<InquiryStatus> StatusOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await inquiries.GetAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        Inquiry = item;
        NewStatus = item.Status;
        StatusOptions = InquiryStatusRules.OptionsFor(item.Status);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await inquiries.UpdateStatusAsync(Inquiry.Id, NewStatus, Inquiry.RowVersion, cancellationToken);
        if (result.NotFound)
        {
            return NotFound();
        }

        Inquiry = result.Inquiry ?? Inquiry;
        NewStatus = Inquiry.Status;
        StatusOptions = InquiryStatusRules.OptionsFor(Inquiry.Status);

        if (result.ConcurrencyConflict || result.ValidationFailed)
        {
            TempData["Error"] = result.Error;
            return Page();
        }

        TempData["Message"] = "הסטטוס עודכן.";
        return RedirectToPage(new { id = Inquiry.Id });
    }
}
