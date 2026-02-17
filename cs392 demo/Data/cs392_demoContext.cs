using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using cs392_demo.models;

namespace cs392_demo.Data
{
    public class cs392_demoContext : DbContext
    {
        public cs392_demoContext (DbContextOptions<cs392_demoContext> options)
            : base(options)
        {
        }

        public DbSet<cs392_demo.models.TestModelClass> TestModelClass { get; set; } = default!;
        public DbSet<cs392_demo.models.Inventory_Location> Inventory_Location { get; set; } = default!;
    }
}
