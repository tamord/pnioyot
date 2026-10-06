using Microsoft.AspNetCore.Mvc.RazorPages;
using Pnioyot.Web.Services;
using Pnioyot.Web.ViewModels;

namespace Pnioyot.Web.Pages;

public class IndexModel(InquiryService inquiries) : PageModel
{
    public DashboardStats Stats { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await inquiries.GetDashboardAsync(cancellationToken);
    }
}
