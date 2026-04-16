using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace cs392_demo.Pages.Stock_Page
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly cs392_demoContext _context;

        public IndexModel(cs392_demoContext context)
        {
            _context = context;
        }

        public IList<Stock> Stock { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 12;
        public int TotalItemsCount { get; set; }
        public int TotalPages { get; set; }
        public int PageStartItem { get; set; }
        public int PageEndItem { get; set; }

        public int AllItemsCount { get; set; }
        public int LowItemsCount { get; set; }
        public int InStockItemsCount { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                Stock = new List<Stock>();
                return;
            }

            var query = _context.Stock
                .Where(s => s.BusinessId == businessId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(s =>
                    s.Item_Name.ToLower().Contains(term) ||
                    s.SKU.ToLower().Contains(term) ||
                    s.Stock_ID.ToLower().Contains(term) ||
                    s.Location_Stock_ID.ToLower().Contains(term) ||
                    (s.Last_Updated_by != null && s.Last_Updated_by.ToLower().Contains(term))
                );
            }

            var scopedStock = await query
                .OrderBy(s => s.Item_Name)
                .ToListAsync();

            AllItemsCount = scopedStock.Count;
            LowItemsCount = scopedStock.Count(s => s.Amount <= s.Danger_Range);
            InStockItemsCount = AllItemsCount - LowItemsCount;

            var normalizedStatus = (Status ?? "all").Trim().ToLowerInvariant();
            if (normalizedStatus != "all" && normalizedStatus != "low" && normalizedStatus != "in")
            {
                normalizedStatus = "all";
            }

            Status = normalizedStatus;

            var filteredStock = normalizedStatus switch
            {
                "low" => scopedStock.Where(s => s.Amount <= s.Danger_Range).ToList(),
                "in" => scopedStock.Where(s => s.Amount > s.Danger_Range).ToList(),
                _ => scopedStock
            };

            TotalItemsCount = filteredStock.Count;
            TotalPages = TotalItemsCount == 0 ? 0 : (int)Math.Ceiling(TotalItemsCount / (double)PageSize);

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            if (TotalPages > 0 && PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            var skip = (PageNumber - 1) * PageSize;
            Stock = filteredStock
                .Skip(skip)
                .Take(PageSize)
                .ToList();

            if (TotalItemsCount == 0)
            {
                PageStartItem = 0;
                PageEndItem = 0;
                PageNumber = 1;
            }
            else
            {
                PageStartItem = skip + 1;
                PageEndItem = Math.Min(skip + PageSize, TotalItemsCount);
            }
        }
    }
}
