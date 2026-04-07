using cs392_demo.models;
using MongoDB.Driver;

namespace cs392_demo
{
    public class MongoDBServices
    {
        private readonly IMongoCollection<InventoryLog> _inventoryLogs;

        public MongoDBServices(IConfiguration config)
        {
            var client = new MongoClient(
                config["mongoDBSettings:ConnectionString"]
                );
            var database = client.GetDatabase(
                config["mongoDBSettings:DatabaseName"]
                );

            _inventoryLogs = database.GetCollection<InventoryLog>(
                config["mongoDBSettings:CollectionName"]
                );
        }
        public async Task<List<InventoryLog>> GetAllAsync()
        { 
            return await _inventoryLogs.Find(_ => true).ToListAsync();
        }
    }
}

/*
 TODO 
add back end for the inventory modifier page to record this data
add a new page to display the inventory logs, with graphs and shiiiiw
 */