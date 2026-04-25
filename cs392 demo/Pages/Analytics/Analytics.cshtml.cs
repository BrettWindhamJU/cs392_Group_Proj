using System.Security.Claims;
using System.Text.Json;
using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Services;
using MongoDB.Driver;

public class AnalyticsModel : PageModel
{
    private readonly cs392_demoContext _context;
    private readonly MongoDBService _mongoService;
    public AnalyticsModel(cs392_demoContext context, MongoDBService mongoService)
    {
        _context = context;
        _mongoService = mongoService;
    }

    [BindProperty(SupportsGet = true)] public string? FilterLocation { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterItem { get; set; }
    [BindProperty(SupportsGet = true)] public long? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public long? EndDate { get; set; }

    public List<LocationOption> LocationOptions { get; set; } = new();
    public List<string> ItemOptions { get; set; } = new();
    public string StockDataJson { get; set; } = "[]";
    public long MinDate { get; set; }
    public long MaxDate { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var businessId = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.BusinessId).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(businessId)) return;

        var stocksQ = _context.Stock.AsNoTracking().Where(s => s.BusinessId == businessId);
        if (!string.IsNullOrWhiteSpace(FilterLocation))
            stocksQ = stocksQ.Where(s => s.Location_Stock_ID == FilterLocation);
        var stocks = await stocksQ.OrderBy(s => s.Item_Name).ToListAsync();
        ItemOptions = stocks.Select(s => s.Item_Name).Distinct().OrderBy(n => n).ToList();
        if (!string.IsNullOrWhiteSpace(FilterItem))
            stocks = stocks.Where(s => s.Item_Name == FilterItem).ToList();

        var locations = await _context.Inventory_Location.AsNoTracking()
            .Where(l => l.BusinessId == businessId)
            .OrderBy(l => l.Location_name)
            .Select(l => new LocationOption { Id = l.location_id, Name = l.Location_name }).ToListAsync();
        LocationOptions = locations;

        var stockIds = stocks.Select(s => s.Stock_ID).ToHashSet();
        var logsQ = _context.Inventory_Activity_Log.AsNoTracking()
            .Where(l => l.BusinessId == businessId && stockIds.Contains(l.Stock_ID_Log));

        // MongoDB logs
        var mongoLogs = await _mongoService.InventoryLog.Find(l => l.BusinessId == businessId && stockIds.Contains(l.Stock_ID_Log)).ToListAsync();

        // Find min/max date for slider (from both sources)
        var sqlMin = await logsQ.OrderBy(l => l.Changed_At).FirstOrDefaultAsync();
        var sqlMax = await logsQ.OrderByDescending(l => l.Changed_At).FirstOrDefaultAsync();
        var mongoMin = mongoLogs.OrderBy(l => l.Changed_At).FirstOrDefault();
        var mongoMax = mongoLogs.OrderByDescending(l => l.Changed_At).FirstOrDefault();
        var minDates = new List<DateTime>();
        var maxDates = new List<DateTime>();
        if (sqlMin != null) minDates.Add(sqlMin.Changed_At);
        if (mongoMin != null) minDates.Add(mongoMin.Changed_At);
        if (sqlMax != null) maxDates.Add(sqlMax.Changed_At);
        if (mongoMax != null) maxDates.Add(mongoMax.Changed_At);
        if (minDates.Count == 0 || maxDates.Count == 0) return;
        MinDate = new DateTimeOffset(minDates.Min()).ToUnixTimeMilliseconds();
        MaxDate = new DateTimeOffset(maxDates.Max()).ToUnixTimeMilliseconds();

        if (StartDate.HasValue && EndDate.HasValue)
        {
            var start = DateTimeOffset.FromUnixTimeMilliseconds(StartDate.Value).UtcDateTime;
            var end = DateTimeOffset.FromUnixTimeMilliseconds(EndDate.Value).UtcDateTime;
            logsQ = logsQ.Where(l => l.Changed_At >= start && l.Changed_At <= end);
            mongoLogs = mongoLogs.Where(l => l.Changed_At >= start && l.Changed_At <= end).ToList();
        }

        var sqlLogs = await logsQ.OrderBy(l => l.Changed_At).ToListAsync();
        // Merge logs
        var allLogs = sqlLogs.Select(l => new CombinedLog
        {
            Stock_ID_Log = l.Stock_ID_Log,
            Changed_At = l.Changed_At,
            Quantity_After = l.Quantity_After
        }).ToList();
        allLogs.AddRange(mongoLogs.Select(l => new CombinedLog
        {
            Stock_ID_Log = l.Stock_ID_Log,
            Changed_At = l.Changed_At,
            Quantity_After = l.Quantity_After
        }));

        var stockById = stocks.GroupBy(s => s.Stock_ID).ToDictionary(g => g.Key, g => g.First());
        var chartData = allLogs
            .GroupBy(l => l.Stock_ID_Log)
            .Where(g => stockById.ContainsKey(g.Key))
            .Select(g =>
            {
                var s = stockById[g.Key];
                return new
                {
                    name = s.Item_Name,
                    data = g.OrderBy(l => l.Changed_At).Select(l => new
                    {
                        date = new DateTimeOffset(DateTime.SpecifyKind(l.Changed_At, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                        amount = l.Quantity_After
                    })
                };
            })
            .OrderBy(x => x.name)
            .ToList();

        StockDataJson = JsonSerializer.Serialize(chartData);
    }

public class CombinedLog
{
    public string Stock_ID_Log { get; set; } = string.Empty;
    public DateTime Changed_At { get; set; }
    public int Quantity_After { get; set; }
}

public class LocationOption { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }
}