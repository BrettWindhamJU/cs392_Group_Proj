using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Users
    {
        public string Username { get; set; }

        public string Password_account { get; set; }

        [Key]
        public char User_ID { get; set; }

        public string Email { get; set; }

        public string Role_Database{ get; set; }

    }
}
