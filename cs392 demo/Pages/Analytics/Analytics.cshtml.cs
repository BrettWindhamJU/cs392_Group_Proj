using cs392_demo.models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class AnalyticsModel : PageModel
{
    private readonly cs392_demo.Data.cs392_demoContext _context;
    public AnalyticsModel(cs392_demo.Data.cs392_demoContext context)
    {
        _context = context;
    }

    public IList<Inventory_Activity_Log> Inventory_Activity_Log { get; set; } = default!;

    public string StockDataJson { get; set; } = string.Empty;

    public void OnGet()
    {
        // Load data from database (if not already loaded)
        Inventory_Activity_Log = _context.Inventory_Activity_Log.ToList();

        var stockData = Inventory_Activity_Log
            .GroupBy(log => log.Stock_ID_Log)
            .ToDictionary(
                group => group.Key.ToString(),
                group => group
                    .OrderBy(log => log.Changed_At) // adjust property name if needed
                    .Select(log => new StockPoint
                    {
                        date = log.Changed_At,      // adjust if your field name differs
                        amount = log.Quantity_After   // adjust if your field name differs
                    })
                    .ToList()
            );

        StockDataJson = JsonSerializer.Serialize(
            stockData.ToDictionary(
                k => k.Key,
                v => v.Value.Select(p => new {
                    date = new DateTimeOffset(p.date).ToUnixTimeMilliseconds(),
                    amount = p.amount
                })
            )
        );
    }
}
public class StockPoint
{
    public DateTime date { get; set; }
    public int amount { get; set; }
}