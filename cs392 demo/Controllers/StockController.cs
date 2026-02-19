using cs392_demo.models;
using cs392_demo.viewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class StockController : Controller
{
    private readonly cs392_demo.Data.cs392_demoContext _context;

    public StockController(cs392_demo.Data.cs392_demoContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var data = (from stock in _context.Stock
                    join location in _context.Inventory_Location
                    on stock.Location_Stock_ID.ToString() equals location.location_id
                    select new StockInventoryViewModel
                    {
                        // Stock
                        Stock_ID = stock.Stock_ID,
                        Location_Stock_ID = stock.Location_Stock_ID,
                        Item_Name = stock.Item_Name,
                        SKU = stock.SKU,
                        Amount = stock.Amount,
                        Danger_Range = stock.Danger_Range,
                        Last_Updated = stock.Last_Updated,
                        Last_Updated_by = stock.Last_Updated_by,

                        // Location
                        Location_Id = location.location_id,
                        Location_Name = location.Location_name,
                        Address_Location = location.Address_Location,
                        Owner_User_ID = location.Owner_User_ID
                    }).ToList();

        return View(data);
    }
}
