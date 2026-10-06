using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pnioyot.Web.Models;
using Pnioyot.Web.Services;
using Pnioyot.Web.ViewModels;

namespace Pnioyot.Web.Pages.Inquiries;

public class IndexModel(InquiryService inquiries) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public InquiryFilter Filter { get; set; } = new();

    public PagedResult<Inquiry> Result { get; private set; } = new()
    {
        Items = [],
        TotalCount = 0,
        Page = 1,
        PageSize = 25
    };

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Result = await inquiries.SearchAsync(Filter, cancellationToken);
    }
}
