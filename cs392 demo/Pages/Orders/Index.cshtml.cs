using cs392_demo.Data;
using cs392_demo.models;
using cs392_demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace cs392_demo.Pages.Orders
{
    [Authorize(Roles = "Owner,Manager")]
    public class IndexModel : PageModel
    {
        private readonly cs392_demoContext _context;
        private readonly MongoDBService _mongoService;

        public IndexModel(cs392_demoContext context, MongoDBService mongoService)
        {
            _context = context;
            _mongoService = mongoService;
        }

        public List<PurchaseOrder> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SupplierFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 12;
        public int TotalOrdersCount { get; set; }
        public int TotalPages { get; set; }
        public int PageStartItem { get; set; }
        public int PageEndItem { get; set; }

        // Counts per status for the chips
        public int DraftCount { get; set; }
        public int ActiveCount { get; set; }
        public int ReceivedCount { get; set; }
        public int CancelledCount { get; set; }

        public List<string> SupplierNames { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (string.IsNullOrWhiteSpace(businessId))
            {
                Orders = new();
                return;
            }

            // Supplier names list for filter dropdown
            try
            {
                var suppliers = await _mongoService.GetByBusinessAsync(businessId);
                SupplierNames = suppliers.Select(s => s.Name).OrderBy(n => n).ToList();
            }
            catch { }

            var allOrders = _context.PurchaseOrder
                .Include(o => o.LineItems)
                .Where(o => o.BusinessId == businessId);

            // Status counts
            DraftCount = await allOrders.CountAsync(o => o.Status == PurchaseOrderStatus.Draft);
            ActiveCount = await allOrders.CountAsync(o => o.Status == PurchaseOrderStatus.Submitted
                                                       || o.Status == PurchaseOrderStatus.Approved
                                                       || o.Status == PurchaseOrderStatus.Ordered
                                                       || o.Status == PurchaseOrderStatus.PartiallyReceived);
            ReceivedCount = await allOrders.CountAsync(o => o.Status == PurchaseOrderStatus.Received);
            CancelledCount = await allOrders.CountAsync(o => o.Status == PurchaseOrderStatus.Cancelled);

            var query = allOrders.AsQueryable();

            // Filters
            if (StatusFilter == "Active")
            {
                query = query.Where(o => o.Status == PurchaseOrderStatus.Submitted
                                      || o.Status == PurchaseOrderStatus.Approved
                                      || o.Status == PurchaseOrderStatus.Ordered
                                      || o.Status == PurchaseOrderStatus.PartiallyReceived);
            }
            else if (!string.IsNullOrWhiteSpace(StatusFilter) && Enum.TryParse<PurchaseOrderStatus>(StatusFilter, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(SupplierFilter))
                query = query.Where(o => o.SupplierName == SupplierFilter);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLower();
                query = query.Where(o =>
                    o.PONumber.ToLower().Contains(q) ||
                    o.SupplierName.ToLower().Contains(q) ||
                    (o.Notes != null && o.Notes.ToLower().Contains(q)));
            }

            if (DateFrom.HasValue)
                query = query.Where(o => o.CreatedAt >= DateFrom.Value);

            if (DateTo.HasValue)
                query = query.Where(o => o.CreatedAt <= DateTo.Value.AddDays(1));

            query = query.OrderByDescending(o => o.CreatedAt);

            TotalOrdersCount = await query.CountAsync();
            TotalPages = TotalOrdersCount == 0 ? 0 : (int)Math.Ceiling(TotalOrdersCount / (double)PageSize);

            if (PageNumber < 1) PageNumber = 1;
            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

            var skip = (PageNumber - 1) * PageSize;
            PageStartItem = TotalOrdersCount == 0 ? 0 : skip + 1;
            PageEndItem = Math.Min(skip + PageSize, TotalOrdersCount);

            Orders = await query.Skip(skip).Take(PageSize).ToListAsync();
        }
    }
}
