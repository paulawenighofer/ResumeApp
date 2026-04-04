using API.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace API.Pages;

public class DashboardModel : PageModel
{
    private readonly DashboardVisitStore _visitStore;

    public DashboardModel(DashboardVisitStore visitStore)
    {
        _visitStore = visitStore;
    }

    public async Task OnGetAsync()
    {
        await _visitStore.RecordVisitAsync(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
