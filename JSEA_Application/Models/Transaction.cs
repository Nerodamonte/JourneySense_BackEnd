using JSEA_Application.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSEA_Application.Models;

[Table("transactions")]
public partial class Transaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("amount")]
    public long? Amount { get; set; }

    /// <summary>
    /// Lưu thông tin gói tại thời điểm mua
    /// </summary>
    [Column("order_code")]
    public long? OrderCode { get; set; } 

    [Column("payment_link_id")]
    public string? PaymentLinkId { get; set; }

    [Column("checkout_url")]
    public string? CheckoutUrl { get; set; }

    [Column("webhook_data", TypeName = "jsonb")]
    public string? WebhookData { get; set; } 

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }
    [Column("item_snapshot", TypeName = "jsonb")]
    public string? ItemSnapshot { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Transactions")]
    public virtual User? User { get; set; }

    [Column("type")]
    public TransactionType Type { get; set; }

    [Column("status")]
    public TransactionStatus Status { get; set; }
}
