namespace cs392_demo.viewModels
{
    public class StockItemViewModel
    {
        public char Stock_ID { get; set; }
        public string Item_Name { get; set; }
        public char SKU { get; set; }
        public int Amount { get; set; }
        public int Danger_Range { get; set; }
        public DateTime Last_Updated { get; set; }
    }
}
