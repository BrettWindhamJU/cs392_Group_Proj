using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

public class AnalyticsPrediction : PageModel
{
    private readonly cs392_demoContext _context;
    private readonly AIService _ai;
    private readonly ILogger<AnalyticsPrediction> _logger;

    public AnalyticsPrediction(
        cs392_demoContext context,
        AIService ai,
        ILogger<AnalyticsPrediction> logger)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
    }

    public string PredictionJson { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var logs = await _context.Inventory_Activity_Log.ToListAsync();

        //  LIMIT DATA (prevents Gemini 400 due to size)
        var groupedData = logs
            .GroupBy(log => log.Stock_ID_Log)
            .Take(5) // limit number of stock items
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.OrderByDescending(x => x.Changed_At)
                      .Take(20) // limit history per item
                      .OrderBy(x => x.Changed_At)
                      .Select(x => new
                      {
                          date = new DateTimeOffset(x.Changed_At).ToUnixTimeSeconds(),
                          amount = x.Quantity_After
                      }).ToList<object>()
            );

        var aiContext = BuildStockContext(groupedData);

        _logger.LogInformation("AI Context:\n{context}", aiContext);

        //  Combine prompt + context (Gemini expects one text input)
        var fullPrompt =
            "Predict the next 10 future stock values for each stock item.\n" +
            "Return valid JSON only. No explanations.\n\n" +
            aiContext;

        var aiResponse = await _ai.SendPromptWithContextAsync(
            fullPrompt,
            "", // no separate context anymore
            "Analytics"
        );

        var cleaned = CleanJson(aiResponse);

        PredictionJson = cleaned;

        _logger.LogInformation("Cleaned PredictionJson: {json}", PredictionJson);

        //  Validate JSON structure
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, List<PredictionPoint>>>(cleaned);
            if (parsed == null)
            {
                _logger.LogWarning("Parsed JSON is null.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize AI JSON.");
        }
    }

    private string BuildStockContext(Dictionary<string, List<object>> data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Dataset (JSON format):");

        //  Build PURE JSON structure (no mixed text)
        sb.AppendLine("{");

        int stockCount = 0;
        foreach (var item in data)
        {
            sb.AppendLine($"  \"{item.Key}\": [");

            int pointCount = 0;
            foreach (var point in item.Value)
            {
                var json = JsonSerializer.Serialize(point);
                var comma = pointCount < item.Value.Count - 1 ? "," : "";
                sb.AppendLine($"    {json}{comma}");
                pointCount++;
            }

            var stockComma = stockCount < data.Count - 1 ? "," : "";
            sb.AppendLine($"  ]{stockComma}");

            stockCount++;
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string CleanJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "{}";

        var cleaned = input
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            _logger.LogError("Invalid JSON returned from AI: {response}", cleaned);
            return "{}";
        }
    }
}

public class PredictionPoint
{
    public long date { get; set; }
    public int amount { get; set; }
}