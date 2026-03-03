using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class Inventory_Location
    {
        [Key] public string location_id { get; set; } = string.Empty;
        public string Location_name { get; set; } = string.Empty;
        public string Address_Location { get; set; } = string.Empty;
        public string Owner_User_ID { get; set; } = string.Empty;

        [NotMapped]
        public string Location_ID
        {
            get => location_id;
            set => location_id = value;
        }

        [NotMapped]
        public string Location_Name
        {
            get => Location_name;
            set => Location_name = value;
        }

    }
}
