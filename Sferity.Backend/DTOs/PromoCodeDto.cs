using Sferity.Backend.Models;

namespace Sferity.Backend.DTOs
{
    public class PromoCodeDto
    {
        public Guid Code { get; set; }
        public string? Label { get; set; }
        public bool AllowLabelRedemption { get; set; }
        public int CreditAmount { get; set; }
        public DateTime ActiveFrom { get; set; } 
        public DateTime ExpiresAt { get; set; }
        public PromoCodeStatus Status { get; set; }
        // Fallback flag in case expiry job hasn't run yet.
        // Frontend should treat this as expired even if Status is still Active.
        public bool IsPendingExpiry => Status == PromoCodeStatus.Active && ExpiresAt < DateTime.UtcNow;
    }
}