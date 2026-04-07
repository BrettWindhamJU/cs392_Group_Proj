using MongoDB.Driver;
using cs392_demo.models;
using Microsoft.Extensions.Configuration;

namespace cs392_demo.Services
{
    public class MongoDBServices
    {
        private readonly IMongoDatabase _database;

        public MongoDBServices(IConfiguration config)
        {
            // Read config safely
            var connectionString = config["mongoDBSettings:ConnectionString"];
            var databaseName = config["mongoDBSettings:DatabaseName"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("MongoDB connection string is missing in appsettings.json under mongoDBSettings:ConnectionString");

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new Exception("MongoDB database name is missing in appsettings.json under mongoDBSettings:DatabaseName");

            // Create client and get database
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Expose collections safely
        public IMongoCollection<InventoryLog> InventoryLog =>
            _database.GetCollection<InventoryLog>("InventoryLog");

    }
}