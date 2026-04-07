using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        public string? Role_Database { get; set; }

        /// <summary>The business this user belongs to.</summary>
        public string? BusinessId { get; set; }

        [ForeignKey(nameof(BusinessId))]
        public Business? Business { get; set; }
    }
}