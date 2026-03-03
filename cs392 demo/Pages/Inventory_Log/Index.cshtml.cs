using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public IList<Inventory_Activity_Log> Inventory_Activity_Log { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Inventory_Activity_Log = await _context.Inventory_Activity_Log.ToListAsync();
        }
    }
}
