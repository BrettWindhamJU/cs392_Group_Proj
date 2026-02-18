using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class Inventory_Activity_Log
    {
        public char Log_ID { get; set; }

        [Key]

        public char Stock_ID_Log { get; set; }
        public int Quantity_Before { get; set; }
        public int Quantity_After { get; set; }
        public char Changed_By { get; set; }
        public DateTime Changed_At { get; set; }


    }
}
