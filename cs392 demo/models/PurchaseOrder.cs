using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
    public class PurchaseOrder
    {
        [Key]
        public int Id { get; set; }

        // ── Business scoping ────────────────────────────────────────────
        [Required]
        public string BusinessId { get; set; } = string.Empty;

        // ── PO identity ─────────────────────────────────────────────────
        [Display(Name = "PO Number")]
        public string PONumber { get; set; } = string.Empty;

        // ── Supplier ─────────────────────────────────────────────────────
        // MongoDB _id of the supplier
        [Required(ErrorMessage = "Please select a supplier.")]
        [Display(Name = "Supplier")]
        public string SupplierMongoId { get; set; } = string.Empty;

        // Denormalized supplier name so we can display it without a Mongo round trip
        [Required]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        // ── Status & dates ───────────────────────────────────────────────
        [Display(Name = "Status")]
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Submitted At")]
        public DateTime? SubmittedAt { get; set; }

        [Display(Name = "Approved At")]
        public DateTime? ApprovedAt { get; set; }

        [Display(Name = "Expected Delivery")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "Received At")]
        public DateTime? ReceivedAt { get; set; }

        // ── Who did what ─────────────────────────────────────────────────
        [Display(Name = "Created By")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Display(Name = "Created By")]
        public string CreatedByUserName { get; set; } = string.Empty;

        [Display(Name = "Approved By")]
        public string? ApprovedByUserId { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedByUserName { get; set; }

        [Display(Name = "Received By")]
        public string? ReceivedByUserId { get; set; }

        [Display(Name = "Received By")]
        public string? ReceivedByUserName { get; set; }

        // ── Delivery Location ─────────────────────────────────────────────
        // location_id of the Inventory_Location this order is destined for
        [Display(Name = "Destination Location")]
        public string? LocationId { get; set; }

        // ── Notes ────────────────────────────────────────────────────────
        public string? Notes { get; set; }

        // ── Navigation ───────────────────────────────────────────────────
        public ICollection<PurchaseOrderLineItem> LineItems { get; set; } = new List<PurchaseOrderLineItem>();

        // ── Computed (not mapped) ─────────────────────────────────────────
        [NotMapped]
        public decimal TotalAmount => LineItems.Sum(li => li.Subtotal);

        [NotMapped]
        public bool IsEditable => Status == PurchaseOrderStatus.Draft || Status == PurchaseOrderStatus.Submitted;

        [NotMapped]
        public bool IsCancellable => Status != PurchaseOrderStatus.Received && Status != PurchaseOrderStatus.Cancelled;

        [NotMapped]
        public bool CanReceive => Status == PurchaseOrderStatus.Ordered
                               || Status == PurchaseOrderStatus.PartiallyReceived;
    }
}
