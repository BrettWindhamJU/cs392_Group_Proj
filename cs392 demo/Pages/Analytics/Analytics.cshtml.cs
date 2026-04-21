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
        // Load data from database
        Inventory_Activity_Log = _context.Inventory_Activity_Log.ToList();
        var stocks = _context.Stock.ToList();

        // Create lookup: Stock_ID -> Stock Name
        var stockNameLookup = stocks
            .DistinctBy(s => s.Stock_ID)
            .ToDictionary(s => s.Stock_ID, s => s.Item_Name);

        // Group logs by Stock ID and attach stock names
        var stockData = Inventory_Activity_Log
            .GroupBy(log => log.Stock_ID_Log)
            .ToDictionary(
                group => group.Key.ToString(),
                group => new
                {
                    Name = stockNameLookup.ContainsKey(group.Key)
                        ? stockNameLookup[group.Key]
                        : "Unknown Item",

                    Data = group
                        .OrderBy(log => log.Changed_At)
                        .Select(log => new StockPoint
                        {
                            date = log.Changed_At,
                            amount = log.Quantity_After
                        })
                        .ToList()
                }
            );

        // Serialize for frontend usage
        StockDataJson = JsonSerializer.Serialize(
            stockData.ToDictionary(
                k => k.Key,
                v => new
                {
                    name = v.Value.Name,
                    data = v.Value.Data.Select(p => new
                    {
                        date = new DateTimeOffset(p.date).ToUnixTimeMilliseconds(),
                        amount = p.amount
                    })
                }
            )
        );
    }
}

public class StockPoint
{
    public DateTime date { get; set; }
    public int amount { get; set; }
}