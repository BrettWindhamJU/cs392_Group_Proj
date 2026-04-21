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
using System.Globalization;
using System.Text.RegularExpressions;

namespace CS392_Demo3.Pages.Curriculum
{
    public class ChatbotModel : PageModel
    {
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
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        public List<ChatMessage> ChatHistory { get; set; } = new();
        public List<cs392_demo.models.ChatSession> PastSessions { get; set; } = new();
        public cs392_demo.models.ChatSession? ActiveSession { get; set; }

        [BindProperty]
        public string UserMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? SessionId { get; set; }

        public bool IsProcessing { get; private set; }

        public async Task<IActionResult> OnPostDeleteSessionAsync(int deleteId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await _context.ChatSession.FirstOrDefaultAsync(s => s.Id == deleteId && s.UserId == userId);
            if (session != null)
            {
                var messages = _context.ChatMessage.Where(m => m.ChatSessionId == deleteId);
                _context.ChatMessage.RemoveRange(messages);
                _context.ChatSession.Remove(session);
                await _context.SaveChangesAsync();
            }
            // If we deleted the active session, go to fresh chat
            var redirectId = SessionId == deleteId ? (int?)null : SessionId;
            return RedirectToPage(new { sessionId = redirectId });
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = user?.BusinessId;
            if (string.IsNullOrWhiteSpace(businessId)) return;

            PastSessions = await _context.ChatSession
                .Where(s => s.UserId == userId && s.BusinessId == businessId)
                .OrderByDescending(s => s.UpdatedAt)
                .Take(30)
                .ToListAsync();

            if (SessionId.HasValue)
            {
                ActiveSession = PastSessions.FirstOrDefault(s => s.Id == SessionId.Value);
                if (ActiveSession != null)
                {
                    var msgs = await _context.ChatMessage
                        .Where(m => m.ChatSessionId == ActiveSession.Id)
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();
                    ChatHistory = msgs.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList();
                }
            }
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            // Start a new chat by redirecting with no session ID
            return RedirectToPage(new { sessionId = (int?)null });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;

            try
            {
                if (string.IsNullOrWhiteSpace(UserMessage))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a message.");
                    await OnGetAsync();
                    return Page();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var businessId = user?.BusinessId ?? string.Empty;

                var userQuestion = UserMessage.Trim();

                // Get or create persistent session
                cs392_demo.models.ChatSession? session = null;
                if (SessionId.HasValue)
                    session = await _context.ChatSession.FirstOrDefaultAsync(s => s.Id == SessionId.Value && s.UserId == userId);

                if (session == null)
                {
                    session = new cs392_demo.models.ChatSession
                    {
                        UserId = userId ?? string.Empty,
                        BusinessId = businessId,
                        Title = userQuestion.Length > 60 ? userQuestion[..60] + "…" : userQuestion,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ChatSession.Add(session);
                    await _context.SaveChangesAsync();
                    SessionId = session.Id;
                }

                // Save user message to DB
                _context.ChatMessage.Add(new cs392_demo.models.ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "User",
                    Content = userQuestion,
                    CreatedAt = DateTime.UtcNow
                });

                // Load history for context
                var dbMessages = await _context.ChatMessage
                    .Where(m => m.ChatSessionId == session.Id)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
                ChatHistory = dbMessages.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList();

                string response;
                string supplierContext;
                try
                {
                    supplierContext = await BuildSupplierContextAsync(userQuestion);
                }
                catch (System.Exception ex) when (IsMongoConnectionIssue(ex))
                {
                    _logger.LogWarning(ex, "MongoDB is unavailable while building supplier context.");
                    response = "I can help with supplier questions, but I cannot reach the supplier database right now. Please try again in a moment.";

                    _context.ChatMessage.Add(new cs392_demo.models.ChatMessage { ChatSessionId = session.Id, Role = "AI", Content = response, CreatedAt = DateTime.UtcNow });
                    session.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    UserMessage = string.Empty;
                    return RedirectToPage(new { sessionId = session.Id });
                }

                if (string.IsNullOrWhiteSpace(supplierContext))
                {
                    response = "No supplier data is available for your business yet.";
                }
                else
                {
                    try
                    {
                        response = await _ai.SendPromptWithContextAsync(userQuestion, supplierContext, null);
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

                // Save AI response to DB
                _context.ChatMessage.Add(new cs392_demo.models.ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "AI",
                    Content = response,
                    CreatedAt = DateTime.UtcNow
                });
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                UserMessage = string.Empty;

                return RedirectToPage(new { sessionId = session.Id });
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

        private async Task<string> BuildSupplierContextAsync(string userQuestion)
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

            var relevantSuppliers = GetRelevantSuppliers(suppliers, userQuestion);

            var sb = new StringBuilder();
            sb.AppendLine("Supplier dataset context");
            sb.AppendLine("Only answer using this supplier dataset. If a requested value is not present, say it is not available in current supplier records.");
            sb.AppendLine();
            sb.AppendLine($"Business ID: {businessId}");
            sb.AppendLine($"Supplier count: {suppliers.Count}");
            sb.AppendLine($"Context suppliers included: {relevantSuppliers.Count}");
            sb.AppendLine($"Active suppliers: {suppliers.Count(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase))}");
            sb.AppendLine($"Inactive suppliers: {suppliers.Count(s => string.Equals(s.Status, "inactive", StringComparison.OrdinalIgnoreCase))}");
            sb.AppendLine();
            sb.AppendLine("Suppliers:");

            foreach (var s in relevantSuppliers)
            {
                sb.Append(BuildSupplierSnapshot(s));
                sb.AppendLine();
            }

            return sb.ToString();
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
            var asksForProfile = text.Contains("full profile") || text.Contains("profile") || text.Contains("details");
            var asksForLocation = text.Contains("located") || text.Contains("location") || text.Contains("city") || text.Contains("state") || text.Contains("address");

            var directMatches = FindDirectSupplierMatches(suppliers, userMessage);
            if (directMatches.Count > 0)
            {
                sb.AppendLine(directMatches.Count == 1 ? "Matched supplier" : "Matched suppliers");
                sb.AppendLine();
                foreach (var supplier in directMatches.Take(5))
                {
                    sb.Append(BuildSupplierSnapshot(supplier));
                    sb.AppendLine();
                }
                return sb.ToString().Trim();
            }

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
                sb.AppendLine(locationMatches.Count == 1 ? "Supplier in requested location" : "Suppliers in requested location");
                sb.AppendLine();
                foreach (var s in locationMatches)
                {
                    sb.AppendLine($"- {s.Name} ({s.SupplierId}) - {s.Address?.City}, {s.Address?.State}");
                }
                return sb.ToString().Trim();
            }

            if (asksForProfile)
            {
                return "I could not find a supplier matching that identifier or name in your current supplier records.";
            }

            if (asksForLocation)
            {
                return "I could not find any suppliers matching that location in your current supplier records.";
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

        private static List<Supplier> GetRelevantSuppliers(List<Supplier> suppliers, string question)
        {
            if (suppliers.Count <= 20)
            {
                return suppliers;
            }

            var normalizedQuestion = NormalizeText(question);
            var tokens = Tokenize(normalizedQuestion);
            var wantsAll = normalizedQuestion.Contains("all suppliers", StringComparison.Ordinal)
                || normalizedQuestion.Contains("list suppliers", StringComparison.Ordinal)
                || normalizedQuestion.Contains("every supplier", StringComparison.Ordinal)
                || normalizedQuestion.Contains("overall", StringComparison.Ordinal)
                || normalizedQuestion.Contains("summary", StringComparison.Ordinal);

            if (wantsAll)
            {
                return suppliers.Take(40).ToList();
            }

            var scored = suppliers
                .Select(s => new
                {
                    Supplier = s,
                    Score = ScoreSupplierForQuestion(s, normalizedQuestion, tokens)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Supplier.Name)
                .ToList();

            var highConfidence = scored.Where(x => x.Score >= 2).Select(x => x.Supplier).Take(20).ToList();
            if (highConfidence.Count > 0)
            {
                return highConfidence;
            }

            return scored.Select(x => x.Supplier).Take(20).ToList();
        }

        private static int ScoreSupplierForQuestion(Supplier supplier, string normalizedQuestion, HashSet<string> tokens)
        {
            var score = 0;

            void AddIfContains(string? value, int weight)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                var normalizedValue = NormalizeText(value);
                if (normalizedQuestion.Contains(normalizedValue, StringComparison.Ordinal) ||
                    tokens.Any(t => t.Length >= 3 && normalizedValue.Contains(t, StringComparison.Ordinal)))
                {
                    score += weight;
                }
            }

            AddIfContains(supplier.SupplierId, 8);
            AddIfContains(supplier.Name, 7);
            AddIfContains(supplier.Status, 3);
            AddIfContains(supplier.Contact?.PrimaryName, 3);
            AddIfContains(supplier.Contact?.Email, 4);
            AddIfContains(supplier.Contact?.Phone, 4);
            AddIfContains(supplier.Address?.City, 3);
            AddIfContains(supplier.Address?.State, 3);
            AddIfContains(supplier.Address?.Country, 2);
            AddIfContains(supplier.Terms?.PaymentTerms, 4);

            foreach (var category in supplier.Categories ?? new List<string>())
            {
                AddIfContains(category, 3);
            }

            foreach (var item in supplier.Catalog ?? new List<SupplierCatalogItem>())
            {
                AddIfContains(item.StockId, 3);
                AddIfContains(item.SupplierSku, 5);
                AddIfContains(item.Unit, 1);
            }

            if ((normalizedQuestion.Contains("lead time", StringComparison.Ordinal) ||
                 normalizedQuestion.Contains("delivery", StringComparison.Ordinal)) &&
                supplier.Terms?.LeadTimeDays.HasValue == true)
            {
                score += 2;
            }

            if (normalizedQuestion.Contains("minimum order", StringComparison.Ordinal) &&
                supplier.Terms?.MinimumOrderAmount.HasValue == true)
            {
                score += 2;
            }

            return score;
        }

        private static List<Supplier> FindDirectSupplierMatches(List<Supplier> suppliers, string userQuestion)
        {
            var normalizedQuestion = NormalizeText(userQuestion);
            var supplierTokens = ExtractSupplierIdTokens(normalizedQuestion);

            return suppliers
                .Where(s =>
                    (!string.IsNullOrWhiteSpace(s.SupplierId) && normalizedQuestion.Contains(NormalizeText(s.SupplierId), StringComparison.Ordinal)) ||
                    IsSupplierIdTokenMatch(s.SupplierId, supplierTokens) ||
                    (!string.IsNullOrWhiteSpace(s.Name) && normalizedQuestion.Contains(NormalizeText(s.Name), StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(s.Contact?.Email) && normalizedQuestion.Contains(NormalizeText(s.Contact.Email), StringComparison.Ordinal)) ||
                    (s.Catalog?.Any(c => !string.IsNullOrWhiteSpace(c.SupplierSku) && normalizedQuestion.Contains(NormalizeText(c.SupplierSku), StringComparison.Ordinal)) ?? false))
                .Take(10)
                .ToList();
        }

        private static bool IsSupplierIdTokenMatch(string? supplierId, List<string> questionSupplierTokens)
        {
            if (string.IsNullOrWhiteSpace(supplierId) || questionSupplierTokens.Count == 0)
            {
                return false;
            }

            var normalizedId = NormalizeText(supplierId);
            var idDigits = DigitsOnly(normalizedId);

            foreach (var token in questionSupplierTokens)
            {
                if (normalizedId.Contains(token, StringComparison.Ordinal))
                {
                    return true;
                }

                var tokenDigits = DigitsOnly(token);
                if (string.IsNullOrWhiteSpace(tokenDigits) || string.IsNullOrWhiteSpace(idDigits))
                {
                    continue;
                }

                // Match numeric supplier identifiers with or without leading digits/zeros, e.g. SUP-1003 ~= SUP-003.
                if (idDigits == tokenDigits ||
                    idDigits.EndsWith(tokenDigits, StringComparison.Ordinal) ||
                    tokenDigits.EndsWith(idDigits, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> ExtractSupplierIdTokens(string normalizedQuestion)
        {
            var matches = Regex.Matches(normalizedQuestion, @"\b(?:sup(?:plier)?[-_\s]*)?\d{2,6}\b|\bsup[-_]?\d{2,6}\b", RegexOptions.IgnoreCase);
            return matches
                .Select(m => NormalizeText(m.Value))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string DigitsOnly(string value)
        {
            var chars = value.Where(char.IsDigit).ToArray();
            return new string(chars);
        }

        private static HashSet<string> Tokenize(string text)
        {
            var stopWords = new HashSet<string>(StringComparer.Ordinal)
            {
                "the", "a", "an", "and", "or", "to", "for", "of", "in", "on", "at", "by", "with", "from",
                "show", "tell", "give", "what", "which", "who", "is", "are", "was", "were", "do", "does",
                "supplier", "suppliers", "vendor", "vendors", "about", "me", "please"
            };

            return Regex.Split(text, "[^a-z0-9@._-]+")
                .Where(t => t.Length > 1 && !stopWords.Contains(t))
                .ToHashSet(StringComparer.Ordinal);
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static string BuildSupplierSnapshot(Supplier s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SupplierId: {ValueOrDash(s.SupplierId)}");
            sb.AppendLine($"Name: {ValueOrDash(s.Name)}");
            sb.AppendLine($"Status: {ValueOrDash(s.Status)}");
            sb.AppendLine($"ContactName: {ValueOrDash(s.Contact?.PrimaryName)}");
            sb.AppendLine($"ContactEmail: {ValueOrDash(s.Contact?.Email)}");
            sb.AppendLine($"ContactPhone: {ValueOrDash(s.Contact?.Phone)}");
            sb.AppendLine($"AddressLine1: {ValueOrDash(s.Address?.Line1)}");
            sb.AppendLine($"AddressCity: {ValueOrDash(s.Address?.City)}");
            sb.AppendLine($"AddressState: {ValueOrDash(s.Address?.State)}");
            sb.AppendLine($"AddressPostalCode: {ValueOrDash(s.Address?.PostalCode)}");
            sb.AppendLine($"AddressCountry: {ValueOrDash(s.Address?.Country)}");
            sb.AppendLine($"LeadTimeDays: {(s.Terms?.LeadTimeDays.HasValue == true ? s.Terms.LeadTimeDays.Value.ToString(CultureInfo.InvariantCulture) : "-")}");
            sb.AppendLine($"MinimumOrderAmount: {(s.Terms?.MinimumOrderAmount.HasValue == true ? s.Terms.MinimumOrderAmount.Value.ToString(CultureInfo.InvariantCulture) : "-")}");
            sb.AppendLine($"PaymentTerms: {ValueOrDash(s.Terms?.PaymentTerms)}");
            sb.AppendLine($"DeliveryDays: {JoinOrDash(s.Terms?.DeliveryDays)}");
            sb.AppendLine($"Currency: {ValueOrDash(s.Terms?.Currency)}");
            sb.AppendLine($"Categories: {JoinOrDash(s.Categories)}");
            sb.AppendLine($"ReliabilityScore: {(s.Performance?.ReliabilityScore.HasValue == true ? s.Performance.ReliabilityScore.Value.ToString(CultureInfo.InvariantCulture) : "-")}");
            sb.AppendLine($"LastDeliveryAtUtc: {(s.Performance?.LastDeliveryAtUtc.HasValue == true ? s.Performance.LastDeliveryAtUtc.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) : "-")}");
            sb.AppendLine($"CreatedAtUtc: {(s.CreatedAtUtc == default ? "-" : s.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture))}");
            sb.AppendLine($"UpdatedAtUtc: {(s.UpdatedAtUtc == default ? "-" : s.UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture))}");
            sb.AppendLine($"Notes: {ValueOrDash(s.Notes)}");
            sb.AppendLine($"CatalogItemsCount: {s.Catalog?.Count ?? 0}");

            var catalog = s.Catalog ?? new List<SupplierCatalogItem>();
            if (catalog.Count > 0)
            {
                foreach (var item in catalog.Take(8))
                {
                    sb.AppendLine($"CatalogItem: StockId={ValueOrDash(item.StockId)}, SupplierSku={ValueOrDash(item.SupplierSku)}, Unit={ValueOrDash(item.Unit)}, PackSize={ValueOrDash(item.PackSize)}, LastUnitPrice={(item.LastUnitPrice.HasValue ? item.LastUnitPrice.Value.ToString(CultureInfo.InvariantCulture) : "-")}");
                }

                if (catalog.Count > 8)
                {
                    sb.AppendLine($"CatalogItem: +{catalog.Count - 8} more item(s) not listed.");
                }
            }

            return sb.ToString();
        }

        private static string ValueOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static string JoinOrDash(IEnumerable<string>? values)
        {
            if (values == null)
            {
                return "-";
            }

            var cleaned = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            return cleaned.Count == 0 ? "-" : string.Join(", ", cleaned);
        }
    }
}
