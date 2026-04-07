using cs392_demo.models;
using cs392_demo.Data;
using cs392_demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace CS392_Demo3.Pages.Curriculum
{
    public class ChatbotModel : PageModel
    {
        private const string ChatSessionKey = "Chat_Page_Conversation";
        private const int MaxMessagesToKeep = 40;

        private readonly AIService _ai;
        private readonly ILogger<ChatbotModel> _logger;
        private readonly MongoDBService _mongoService;
        private readonly cs392_demoContext _context;

        public ChatbotModel(
            AIService ai,
            ILogger<ChatbotModel> logger,
            MongoDBService mongoService,
            cs392_demoContext context)
        {
            _ai = ai;
            _logger = logger;
            _mongoService = mongoService;
            _context = context;
        }

        public class ChatMessage
        {
            public string Role { get; set; } = string.Empty;  // "User" or "AI"
            public string Content { get; set; } = string.Empty;
        }

        public List<ChatMessage> ChatHistory { get; set; } = new();

        [BindProperty]
        public string UserMessage { get; set; } = string.Empty;

        public bool IsProcessing { get; private set; }

        public void OnGet()
        {
            ChatHistory = LoadChatHistory();
        }

        public IActionResult OnPostClear()
        {
            HttpContext.Session.Remove(ChatSessionKey);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;
            ChatHistory = LoadChatHistory();

            try
            {
                if (string.IsNullOrWhiteSpace(UserMessage))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a message.");
                    return Page();
                }

                var userQuestion = UserMessage.Trim();

                // Add user message
                ChatHistory.Add(new ChatMessage
                {
                    Role = "User",
                    Content = userQuestion
                });

                string response;
                if (!IsSupplierQuestion(userQuestion))
                {
                    response = "I can currently help only with supplier-related questions. Try asking about supplier status, contacts, payment terms, delivery time, minimum order quantity, or supplier catalog items.";
                }
                else
                {
                    string supplierContext;
                    try
                    {
                        supplierContext = await BuildSupplierContextAsync();
                    }
                    catch (System.Exception ex) when (IsMongoConnectionIssue(ex))
                    {
                        _logger.LogWarning(ex, "MongoDB is unavailable while building supplier context.");
                        response = "I can help with supplier questions, but I cannot reach the supplier database right now. Please try again in a moment.";

                        ChatHistory.Add(new ChatMessage
                        {
                            Role = "AI",
                            Content = response
                        });

                        SaveChatHistory(ChatHistory);
                        UserMessage = string.Empty;
                        return RedirectToPage();
                    }

                    if (string.IsNullOrWhiteSpace(supplierContext))
                    {
                        response = "No supplier data is available for your business yet.";
                    }
                    else
                    {
                        try
                        {
                            response = await _ai.SendPromptWithContextAsync(userQuestion, supplierContext);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogWarning(ex, "AI unavailable for supplier question. Falling back to direct MongoDB summary.");
                            try
                            {
                                response = await BuildLocalSupplierFallbackAsync(userQuestion);
                            }
                            catch (System.Exception fallbackEx) when (IsMongoConnectionIssue(fallbackEx))
                            {
                                _logger.LogWarning(fallbackEx, "MongoDB is unavailable during fallback response build.");
                                response = "I can help with supplier questions, but I cannot reach the supplier database right now. Please try again in a moment.";
                            }
                        }
                    }
                }

                // Add AI response
                ChatHistory.Add(new ChatMessage
                {
                    Role = "AI",
                    Content = response
                });

                SaveChatHistory(ChatHistory);

                // Clear input
                UserMessage = string.Empty;

                return RedirectToPage();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Chatbot failed.");
                ModelState.AddModelError(string.Empty, "The chatbot is temporarily unavailable. Please try again shortly.");
                return Page();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private List<ChatMessage> LoadChatHistory()
        {
            try
            {
                var raw = HttpContext.Session.GetString(ChatSessionKey);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return new List<ChatMessage>();
                }

                var history = JsonSerializer.Deserialize<List<ChatMessage>>(raw) ?? new List<ChatMessage>();
                return history;
            }
            catch
            {
                return new List<ChatMessage>();
            }
        }

        private void SaveChatHistory(List<ChatMessage> history)
        {
            if (history.Count > MaxMessagesToKeep)
            {
                history = history.TakeLast(MaxMessagesToKeep).ToList();
            }

            var raw = JsonSerializer.Serialize(history);
            HttpContext.Session.SetString(ChatSessionKey, raw);
            ChatHistory = history;
        }

        private static bool IsMongoConnectionIssue(System.Exception ex)
        {
            var message = ex.ToString();
            return ex is MongoConnectionException
                || ex is TimeoutException
                || message.Contains("timed out", System.StringComparison.OrdinalIgnoreCase)
                || message.Contains("DnsClient", System.StringComparison.OrdinalIgnoreCase)
                || message.Contains("mongod", System.StringComparison.OrdinalIgnoreCase)
                || message.Contains("server selection", System.StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> BuildSupplierContextAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return string.Empty;
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            if (string.IsNullOrWhiteSpace(businessId))
            {
                return string.Empty;
            }

            var suppliers = await _mongoService.GetByBusinessAsync(businessId);
            if (suppliers.Count == 0)
            {
                return "No suppliers found for this business.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Business ID: {businessId}");
            sb.AppendLine($"Supplier count: {suppliers.Count}");

            foreach (var s in suppliers.Take(30))
            {
                sb.AppendLine($"- SupplierId: {s.SupplierId}");
                sb.AppendLine($"  Name: {s.Name}");
                sb.AppendLine($"  Status: {s.Status}");
                sb.AppendLine($"  Contact: {s.Contact.PrimaryName}, {s.Contact.Email}, {s.Contact.Phone}");
                sb.AppendLine($"  Address: {s.Address.Line1}, {s.Address.City}, {s.Address.State} {s.Address.PostalCode}, {s.Address.Country}");
                sb.AppendLine($"  DeliveryTimeDays: {(s.Terms.LeadTimeDays.HasValue ? s.Terms.LeadTimeDays.Value.ToString() : "-")}");
                sb.AppendLine($"  MinimumOrderQuantity: {(s.Terms.MinimumOrderAmount.HasValue ? s.Terms.MinimumOrderAmount.Value.ToString() : "-")}");
                sb.AppendLine($"  PaymentTerms: {s.Terms.PaymentTerms}");
                sb.AppendLine($"  Categories: {string.Join(", ", s.Categories)}");
                sb.AppendLine($"  CatalogItems: {s.Catalog.Count}");
            }

            return sb.ToString();
        }

        private static bool IsSupplierQuestion(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var text = message.ToLowerInvariant();
            var keywords = new[]
            {
                "supplier", "suppliers", "vendor", "vendors",
                "contact", "email", "phone", "payment terms", "net 30",
                "delivery time", "lead time", "minimum order", "catalog",
                "sku", "supplier id", "company",
                "location", "city", "state", "address"
            };

            return keywords.Any(k => text.Contains(k));
        }

        private async Task<string> BuildLocalSupplierFallbackAsync(string userMessage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "I could not access supplier data for the current user.";
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            if (string.IsNullOrWhiteSpace(businessId))
            {
                return "I could not determine your business context for supplier data.";
            }

            var suppliers = await _mongoService.GetByBusinessAsync(businessId);
            if (suppliers.Count == 0)
            {
                return "No supplier data is available for your business yet.";
            }


            var text = userMessage.ToLowerInvariant();
            var sb = new StringBuilder();

            // Location-based filtering
            var cityMatches = suppliers
                .Where(s => !string.IsNullOrWhiteSpace(s.Address?.City) && text.Contains(s.Address.City.ToLowerInvariant()))
                .ToList();
            var stateMatches = suppliers
                .Where(s => !string.IsNullOrWhiteSpace(s.Address?.State) && text.Contains(s.Address.State.ToLowerInvariant()))
                .ToList();

            var locationMatches = cityMatches.Union(stateMatches).Distinct().ToList();
            if (locationMatches.Count > 0)
            {
                foreach (var s in locationMatches)
                {
                    sb.AppendLine($"{s.Name}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("status") || text.Contains("active") || text.Contains("inactive"))
            {
                var activeCount = suppliers.Count(s => string.Equals(s.Status, "active", System.StringComparison.OrdinalIgnoreCase));
                var inactiveCount = suppliers.Count(s => string.Equals(s.Status, "inactive", System.StringComparison.OrdinalIgnoreCase));
                sb.AppendLine($"Supplier status summary ({suppliers.Count} total)");
                sb.AppendLine($"Active: {activeCount}");
                sb.AppendLine($"Inactive: {inactiveCount}");
                sb.AppendLine();
                foreach (var s in suppliers.Take(15))
                {
                    sb.AppendLine($"- {s.Name} ({s.SupplierId}): {s.Status}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("payment"))
            {
                sb.AppendLine("Supplier payment terms");
                sb.AppendLine();
                foreach (var s in suppliers.Take(15))
                {
                    sb.AppendLine($"- {s.Name}: {(string.IsNullOrWhiteSpace(s.Terms.PaymentTerms) ? "-" : s.Terms.PaymentTerms)}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("delivery") || text.Contains("lead time"))
            {
                var ascending = text.Contains("ascending") || text.Contains("asc") || text.Contains("lowest") || text.Contains("fastest");
                var descending = text.Contains("descending") || text.Contains("desc") || text.Contains("highest") || text.Contains("slowest");

                var ordered = suppliers
                    .OrderBy(s => s.Terms.LeadTimeDays.HasValue ? 0 : 1)
                    .ThenBy(s => s.Terms.LeadTimeDays ?? int.MaxValue)
                    .ToList();

                if (descending && !ascending)
                {
                    ordered = ordered
                        .OrderBy(s => s.Terms.LeadTimeDays.HasValue ? 0 : 1)
                        .ThenByDescending(s => s.Terms.LeadTimeDays ?? int.MinValue)
                        .ToList();
                }

                sb.AppendLine(descending && !ascending
                    ? "Supplier delivery time (highest to lowest)"
                    : "Supplier delivery time (lowest to highest)");
                sb.AppendLine();
                foreach (var s in ordered.Take(15))
                {
                    sb.AppendLine($"- {s.Name}: {(s.Terms.LeadTimeDays.HasValue ? s.Terms.LeadTimeDays.Value + " day(s)" : "-")}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("minimum order"))
            {
                sb.AppendLine("Minimum order quantity by supplier");
                sb.AppendLine();
                foreach (var s in suppliers.Take(15))
                {
                    sb.AppendLine($"- {s.Name}: {(s.Terms.MinimumOrderAmount.HasValue ? s.Terms.MinimumOrderAmount.Value.ToString() : "-")}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("contact") || text.Contains("email") || text.Contains("phone"))
            {
                sb.AppendLine("Supplier contact details");
                sb.AppendLine();
                foreach (var s in suppliers.Take(15))
                {
                    sb.AppendLine($"- {s.Name}: {s.Contact.PrimaryName}, {s.Contact.Email}, {s.Contact.Phone}");
                }
                return sb.ToString().Trim();
            }

            if (text.Contains("catalog") || text.Contains("sku"))
            {
                sb.AppendLine("Supplier catalog summary");
                sb.AppendLine();
                foreach (var s in suppliers.Take(15))
                {
                    sb.AppendLine($"- {s.Name}: {s.Catalog.Count} catalog item(s)");
                }
                return sb.ToString().Trim();
            }

            sb.AppendLine($"Supplier overview ({suppliers.Count} total)");
            sb.AppendLine();
            foreach (var s in suppliers.Take(10))
            {
                sb.AppendLine($"- {s.Name} ({s.SupplierId})");
            }
            sb.AppendLine();
            sb.AppendLine("Ask about supplier status, contacts, payment terms, delivery time, or minimum order quantity for more details.");
            return sb.ToString().Trim();
        }
    }
}
