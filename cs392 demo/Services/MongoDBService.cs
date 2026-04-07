using cs392_demo.models;
using MongoDB.Driver;

namespace cs392_demo.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<Supplier> _collection;

        public MongoDBService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDBSettings:ConnectionString"];
            var databaseName = configuration["MongoDBSettings:DatabaseName"];
            var collectionName = configuration["MongoDBSettings:CollectionName"];
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                collectionName = "suppliers";
            }

            if (string.IsNullOrWhiteSpace(connectionString) ||
                string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDBSettings is missing required values.");
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<Supplier>(collectionName);
        }

        public async Task<List<Supplier>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<List<Supplier>> GetByBusinessAsync(string businessId)
        {
            return await _collection.Find(s => s.BusinessId == businessId).ToListAsync();
        }

        public async Task<Supplier?> GetBySupplierIdAsync(string businessId, string supplierId)
        {
            return await _collection
                .Find(s => s.BusinessId == businessId && s.SupplierId == supplierId)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Supplier supplier)
        {
            await _collection.InsertOneAsync(supplier);
        }

        public async Task<bool> UpdateAsync(string businessId, string supplierId, Supplier updatedSupplier)
        {
            var result = await _collection.ReplaceOneAsync(
                s => s.BusinessId == businessId && s.SupplierId == supplierId,
                updatedSupplier);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string businessId, string supplierId)
        {
            var result = await _collection.DeleteOneAsync(
                s => s.BusinessId == businessId && s.SupplierId == supplierId);

            return result.DeletedCount > 0;
        }
    }
}