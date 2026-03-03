using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly cs392_demoContext _context;

    public int TotalStockItems { get; private set; }
    public int LowStockItems { get; private set; }
    public int TotalLogs { get; private set; }
    public int TotalLocations { get; private set; }
    public IList<Inventory_Activity_Log> RecentLogs { get; private set; } = new List<Inventory_Activity_Log>();

    public IndexModel(cs392_demoContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        TotalStockItems = await _context.Stock.CountAsync();
        LowStockItems = await _context.Stock.CountAsync(s => s.Amount <= s.Danger_Range);
        TotalLogs = await _context.Inventory_Activity_Log.CountAsync();
        TotalLocations = await _context.Inventory_Location.CountAsync();

        RecentLogs = await _context.Inventory_Activity_Log
            .OrderByDescending(log => log.Changed_At)
            .Take(5)
            .ToListAsync();
    }
}