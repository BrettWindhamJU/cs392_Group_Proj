using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Inventory_Location
    {
        [Key]
        public char Location_ID { get; set; }

        public char Location_Name { get; set; }

        public string Address_Location { get; set; }

        public char Owner_User_ID { get; set; }


    }
}
