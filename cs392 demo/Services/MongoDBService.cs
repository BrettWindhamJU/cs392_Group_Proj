using cs392_demo.models;
using MongoDB.Driver;

namespace cs392_demo.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Supplier> _supplierCollection;
        private readonly IMongoCollection<InventoryLog> _inventoryLog;

        public MongoDBService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDBSettings:ConnectionString"];
            var databaseName = configuration["MongoDBSettings:DatabaseName"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("MongoDB connection string is missing.");

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new Exception("MongoDB database name is missing.");

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            // Initialize collections
            _supplierCollection = _database.GetCollection<Supplier>("suppliers");
            _inventoryLog = _database.GetCollection<InventoryLog>("InventoryLog");
        }

        // ---------------- SUPPLIER METHODS ----------------

        public async Task<List<Supplier>> GetAllAsync()
        {
            return await _supplierCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<Supplier>> GetByBusinessAsync(string businessId)
        {
            return await _supplierCollection.Find(s => s.BusinessId == businessId).ToListAsync();
        }

        public async Task<Supplier?> GetBySupplierIdAsync(string businessId, string supplierId)
        {
            return await _supplierCollection
                .Find(s => s.BusinessId == businessId && s.SupplierId == supplierId)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Supplier supplier)
        {
            await _supplierCollection.InsertOneAsync(supplier);
        }

        public async Task<bool> UpdateAsync(string businessId, string supplierId, Supplier updatedSupplier)
        {
            var result = await _supplierCollection.ReplaceOneAsync(
                s => s.BusinessId == businessId && s.SupplierId == supplierId,
                updatedSupplier);

            return result.ModifiedCount > 0;
        }
        public async Task<bool> DeleteAsync(string businessId, string supplierId)
        {
            var result = await _supplierCollection.DeleteOneAsync(
                s => s.BusinessId == businessId && s.SupplierId == supplierId);

            return result.DeletedCount > 0;
        }

        // ---------------- INVENTORY LOG ----------------

        public IMongoCollection<InventoryLog> InventoryLog =>
            _inventoryLog;
    }
}