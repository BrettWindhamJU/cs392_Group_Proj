using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using cs392_demo.models;
using cs392_demo.viewModels;


namespace cs392_demo.Data
{
    public class cs392_demoContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<AppUser>
    {
        public cs392_demoContext(DbContextOptions<cs392_demoContext> options)
            : base(options)
        {
        }

        public DbSet<TestModelClass> TestModelClass { get; set; } = default!;
        public DbSet<Inventory_Location> Inventory_Location { get; set; } = default!;
        public DbSet<Inventory_Activity_Log> Inventory_Activity_Log { get; set; } = default!;
        public DbSet<Stock> Stock { get; set; } = default!;
    }
}