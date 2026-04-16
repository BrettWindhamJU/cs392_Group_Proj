using System.Security.Claims;
using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace cs392_demo.Pages.Suppliers
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public IndexModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        public List<Supplier> Suppliers { get; private set; } = new();
        public string CurrentBusinessId { get; private set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 12;
        public int TotalSuppliersCount { get; set; }
        public int TotalPages { get; set; }
        public int PageStartItem { get; set; }
        public int PageEndItem { get; set; }

        [TempData]
        public string? SuppliersError { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;
            CurrentBusinessId = businessId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                Suppliers = new List<Supplier>();
                return;
            }

            try
            {
                Suppliers = await _mongoService.GetByBusinessAsync(businessId);
                Suppliers = Suppliers.OrderBy(s => s.Name).ToList();

                TotalSuppliersCount = Suppliers.Count;
                TotalPages = TotalSuppliersCount == 0 ? 0 : (int)Math.Ceiling(TotalSuppliersCount / (double)PageSize);

                if (PageNumber < 1)
                {
                    PageNumber = 1;
                }

                if (TotalPages > 0 && PageNumber > TotalPages)
                {
                    PageNumber = TotalPages;
                }

                var skip = (PageNumber - 1) * PageSize;
                Suppliers = Suppliers
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();

                if (TotalSuppliersCount == 0)
                {
                    PageStartItem = 0;
                    PageEndItem = 0;
                    PageNumber = 1;
                }
                else
                {
                    PageStartItem = skip + 1;
                    PageEndItem = Math.Min(skip + PageSize, TotalSuppliersCount);
                }
            }
            catch (Exception ex) when (IsMongoConnectionIssue(ex))
            {
                Suppliers = new List<Supplier>();
                TotalSuppliersCount = 0;
                TotalPages = 0;
                PageStartItem = 0;
                PageEndItem = 0;
                SuppliersError = "Suppliers are temporarily unavailable because the database connection timed out. Please try again shortly.";
            }
        }

        private static bool IsMongoConnectionIssue(Exception ex)
        {
            var message = ex.ToString();
            return ex is MongoConnectionException
                || ex is TimeoutException
                || message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || message.Contains("DnsClient", StringComparison.OrdinalIgnoreCase)
                || message.Contains("server selection", StringComparison.OrdinalIgnoreCase)
                || message.Contains("mongod", StringComparison.OrdinalIgnoreCase);
        }
    }
}
