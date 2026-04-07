using cs392_demo;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;

public class AnalyticsModel : PageModel
{
    private readonly MongoDBServices _mongo;

    public AnalyticsModel(MongoDBServices mongo)
    {
        _mongo = mongo;
    }

    public Dictionary<string, List<ChartPoint>> StockData { get; set; } = new();

    public async Task OnGetAsync()
    {
        var logs = await _mongo.InventoryLog
            .Find(_ => true)
            .SortBy(l => l.Changed_At)
            .ToListAsync();

        // Group logs by Stock_ID
        StockData = logs
            .GroupBy(l => l.Stock_ID_Log)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => new ChartPoint
                {
                    Date = l.Changed_At,
                    Amount = l.Quantity_After
                }).ToList()
            );
    }

    public class ChartPoint
    {
        public DateTime Date { get; set; }
        public int Amount { get; set; }
    }
}