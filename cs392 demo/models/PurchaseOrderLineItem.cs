using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cs392_demo.models
{
        public class PurchaseOrderLineItem
        {
        [Key]
        public int Id { get; set; }

        // ── Parent order ─────────────────────────────────────────────────
        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public PurchaseOrder? PurchaseOrder { get; set; }

        // ── Inventory item reference (SQL Stock table) ───────────────────
        /// Stock_ID from the Stock table scoped to same business.
        [Display(Name = "Stock ID")]
        public string? StockId { get; set; }

        // ── Item details (denormalized so history is preserved) ───────────
        [Required(ErrorMessage = "Item name is required.")]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        // ── Quantities ───────────────────────────────────────────────────
        [Range(1, int.MaxValue, ErrorMessage = "Ordered quantity must be at least 1.")]
        [Display(Name = "Qty Ordered")]
        public int QuantityOrdered { get; set; } = 1;

        [Display(Name = "Qty Received")]
        public int QuantityReceived { get; set; } = 0;

        [NotMapped]
        [Display(Name = "Qty Remaining")]
        public int QuantityRemaining => Math.Max(0, QuantityOrdered - QuantityReceived);

        // ── Pricing ──────────────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Unit cost cannot be negative.")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        [NotMapped]
        [Display(Name = "Subtotal")]
        public decimal Subtotal => QuantityOrdered * UnitCost;
    }
}
