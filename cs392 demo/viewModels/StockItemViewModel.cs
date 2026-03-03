namespace cs392_demo.viewModels
{
    public class StockItemViewModel
    {
        public string Stock_ID { get; set; } = string.Empty;
        public string Item_Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int Danger_Range { get; set; }
        public DateTime? Last_Updated { get; set; }
    }
}
