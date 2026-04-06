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

namespace cs392_demo.Pages.Inventory_Log
{

    [Authorize(Roles = "Owner, Manager")]
    public class IndexModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public IndexModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public IList<Inventory_Activity_Log> Inventory_Activity_Log { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        public int AllLogsCount { get; set; }
        public int DecreasedLogsCount { get; set; }
        public int IncreasedLogsCount { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var businessId = currentUser?.BusinessId;

            if (businessId == null)
            {
                Inventory_Activity_Log = new List<Inventory_Activity_Log>();
                return;
            }

            var scopedLogs = await _context.Inventory_Activity_Log
                .Where(l => l.BusinessId == businessId)
                .OrderByDescending(l => l.Changed_At)
                .ToListAsync();

            AllLogsCount = scopedLogs.Count;
            DecreasedLogsCount = scopedLogs.Count(log => log.Quantity_After < log.Quantity_Before);
            IncreasedLogsCount = scopedLogs.Count(log => log.Quantity_After > log.Quantity_Before);

            var normalizedStatus = (Status ?? "all").Trim().ToLowerInvariant();
            if (normalizedStatus != "all" && normalizedStatus != "decreased" && normalizedStatus != "increased")
            {
                normalizedStatus = "all";
            }

            Status = normalizedStatus;

            Inventory_Activity_Log = normalizedStatus switch
            {
                "decreased" => scopedLogs.Where(log => log.Quantity_After < log.Quantity_Before).ToList(),
                "increased" => scopedLogs.Where(log => log.Quantity_After > log.Quantity_Before).ToList(),
                _ => scopedLogs
            };
        }
    }
}
