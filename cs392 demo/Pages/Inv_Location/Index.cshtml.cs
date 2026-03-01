using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using cs392_demo.Data;
using cs392_demo.models;
using Microsoft.AspNetCore.Authorization;

namespace cs392_demo.Pages.Inv_Location
{
    [Authorize(Roles = "Owner, Manager")]

    public class IndexModel : PageModel
    {
        private readonly cs392_demo.Data.cs392_demoContext _context;

        public IndexModel(cs392_demo.Data.cs392_demoContext context)
        {
            _context = context;
        }

        public IList<Inventory_Location> Inventory_Location { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Inventory_Location = await _context.Inventory_Location.ToListAsync();
        }
    }
}
