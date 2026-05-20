using Sferity.Backend.DTOs;
using Sferity.Backend.Models;

namespace Sferity.Backend.Extensions
{
    public static class PromoCodeExtensions
    {
        public static PromoCodeDto ToDto(this PromoCode promo) => new()
        {
            Code = promo.Code,
            Label = promo.Label,
            AllowLabelRedemption =  promo.AllowLabelRedemption,
            CreditAmount = promo.CreditAmount,
            ActiveFrom = promo.ActiveFrom,
            ExpiresAt = promo.ExpiresAt,
            Status = promo.Status
        };
    }
}