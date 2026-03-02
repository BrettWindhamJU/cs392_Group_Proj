
using cs392_demo.models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace cs392_demo.Data
{
    public class ApplicationContext:IdentityDbContext<AppUser>
    {

        public ApplicationContext(DbContextOptions options) : base(options)
        {



        }
    }
}




