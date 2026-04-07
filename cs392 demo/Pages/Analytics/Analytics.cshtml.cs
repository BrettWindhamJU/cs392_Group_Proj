using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

public class AnalyticsModel : PageModel
{
    public string StockDataJson { get; set; }

    public void OnGet()
    {

        // Example: replace with DB call
        var stockData = new Dictionary<string, List<StockPoint>>
        {
            ["Item A"] = new List<StockPoint>
            {
                new StockPoint { date = DateTime.Now.AddDays(-10), amount = 55 },
                new StockPoint { date = DateTime.Now.AddDays(-9), amount = 43 },
                new StockPoint { date = DateTime.Now.AddDays(-8), amount = 40 },
                new StockPoint { date = DateTime.Now.AddDays(-7), amount = 39 },
                new StockPoint { date = DateTime.Now.AddDays(-6), amount = 37 },
                new StockPoint { date = DateTime.Now.AddDays(-5), amount = 32 },
                new StockPoint { date = DateTime.Now.AddDays(-4), amount = 58 }
            },
            ["Item B"] = new List<StockPoint>
            {
                new StockPoint { date = DateTime.Now.AddDays(-10), amount = 25 },
                new StockPoint { date = DateTime.Now.AddDays(-9), amount = 23 },
                new StockPoint { date = DateTime.Now.AddDays(-8), amount = 20 },
                new StockPoint { date = DateTime.Now.AddDays(-7), amount = 19 },
                new StockPoint { date = DateTime.Now.AddDays(-6), amount = 17 },
                new StockPoint { date = DateTime.Now.AddDays(-5), amount = 12 },
                new StockPoint { date = DateTime.Now.AddDays(-4), amount = 38 }
            }
        };

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