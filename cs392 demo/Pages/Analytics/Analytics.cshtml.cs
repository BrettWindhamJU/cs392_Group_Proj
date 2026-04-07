using cs392_demo;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;

public class AnalyticsModel : PageModel
{
    private readonly MongoDBService _mongo;

    public AnalyticsModel(MongoDBService mongo)
    {
        _mongo = mongo;
    }

    public Dictionary<string, List<ChartPoint>> StockData { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Fetch logs sorted by date
        var logs = await _mongo.InventoryLog
            .Find(_ => true)
            .SortBy(l => l.Changed_At)
            .ToListAsync();

        // Group logs by Stock_ID and convert to ChartPoints
        StockData = logs
            .GroupBy(l => l.Stock_ID_Log)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => new ChartPoint
                {
                    Date = l.Changed_At,
                    Amount = l.Quantity_After // ensure this is int or double
                }).ToList()
            );

        // Debug output: print nicely in console
        Console.WriteLine("StockData:");
        foreach (var kvp in StockData)
        {
            Console.WriteLine($"StockID: {kvp.Key}");
            foreach (var point in kvp.Value)
            {
                Console.WriteLine($"  Date: {point.Date:yyyy-MM-dd HH:mm}, Amount: {point.Amount}");
            }
        }
    }

    public class ChartPoint
    {
        public DateTime Date { get; set; }
        public int Amount { get; set; }
    }
}