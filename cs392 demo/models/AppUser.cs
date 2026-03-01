using Microsoft.AspNetCore.Identity;

namespace cs392_demo.models
{
    public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        public string? Role_Database { get; set; }
    }
}