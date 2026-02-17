using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Inventory_Location
    {
        [Key] public string location_id { get; set; }
        public string Location_name { get; set; }
        public string Address_Location { get; set; }
        public string Owner_User_ID { get; set; }

    }
}
