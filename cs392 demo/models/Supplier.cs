using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    [BsonIgnoreExtraElements]
    public class Supplier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("businessId")]
        [Display(Name = "Business ID")]
        public string BusinessId { get; set; } = string.Empty;

        [BsonElement("supplierId")]
        [Display(Name = "Supplier ID")]
        public string SupplierId { get; set; } = string.Empty;

        [BsonElement("name")]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("contact")]
        public SupplierContact Contact { get; set; } = new();

        [BsonElement("address")]
        public SupplierAddress Address { get; set; } = new();

        [BsonElement("categories")]
        public List<string> Categories { get; set; } = new();

        [BsonElement("terms")]
        public SupplierTerms Terms { get; set; } = new();

        [BsonElement("catalog")]
        public List<SupplierCatalogItem> Catalog { get; set; } = new();

        [BsonElement("performance")]
        public SupplierPerformance Performance { get; set; } = new();

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;

        [BsonElement("createdAtUtc")]
        [Display(Name = "Created At")]
        public DateTime CreatedAtUtc { get; set; }

        [BsonElement("updatedAtUtc")]
        [Display(Name = "Updated At")]
        public DateTime UpdatedAtUtc { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SupplierContact
    {
        [BsonElement("primaryName")]
        [Display(Name = "Primary Name")]
        public string PrimaryName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    public class SupplierAddress
    {
        [BsonElement("line1")]
        public string Line1 { get; set; } = string.Empty;

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        [BsonElement("postalCode")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    public class SupplierTerms
    {
        [BsonElement("leadTimeDays")]
        [Display(Name = "Delivery Time (Days)")]
        public int? LeadTimeDays { get; set; }

        [BsonElement("minimumOrderAmount")]
        [Display(Name = "Minimum Order Quantity")]
        public int? MinimumOrderAmount { get; set; }

        [BsonElement("paymentTerms")]
        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; } = string.Empty;

        [BsonElement("deliveryDays")]
        public List<string> DeliveryDays { get; set; } = new();

        [BsonElement("currency")]
        public string Currency { get; set; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
    public class SupplierCatalogItem
    {
        [BsonElement("stockId")]
        [Display(Name = "Stock ID")]
        public string StockId { get; set; } = string.Empty;

        [BsonElement("supplierSku")]
        [Display(Name = "Supplier SKU")]
        public string SupplierSku { get; set; } = string.Empty;

        [BsonElement("unit")]
        public string Unit { get; set; } = string.Empty;

        [BsonElement("packSize")]
        [Display(Name = "Pack Size")]
        public string PackSize { get; set; } = string.Empty;

        [BsonElement("lastUnitPrice")]
        [Display(Name = "Last Unit Price")]
        public decimal? LastUnitPrice { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class SupplierPerformance
    {
        [BsonElement("reliabilityScore")]
        [Display(Name = "Reliability Score")]
        public int? ReliabilityScore { get; set; }

        [BsonElement("lastDeliveryAtUtc")]
        [Display(Name = "Last Delivery At")]
        public DateTime? LastDeliveryAtUtc { get; set; }
    }
}
