using cs392_demo.Data;
using cs392_demo.viewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly cs392_demoContext _context;

    public IndexModel(cs392_demoContext context)
    {
        _context = context;
    }

    public List<LocationStockViewModel> Locations { get; set; }
    public async Task OnGetAsync()
    {
        Locations = await _context.Inventory_Location
            .Select(location => new LocationStockViewModel
            {
                Location_Id = location.location_id,
                Location_Name = location.Location_name,
                Address_Location = location.Address_Location,
                Owner_User_ID = location.Owner_User_ID,

                Stocks = _context.Stock
                    .Where(stock => stock.Location_Stock_ID.ToString() == location.location_id)
                    .Select(stock => new StockItemViewModel
                    {
                        Stock_ID = stock.Stock_ID,
                        Item_Name = stock.Item_Name,
                        SKU = stock.SKU,
                        Amount = stock.Amount,
                        Danger_Range = stock.Danger_Range,
                        Last_Updated = stock.Last_Updated
                    }).ToList()
            }).ToListAsync();
    }
}