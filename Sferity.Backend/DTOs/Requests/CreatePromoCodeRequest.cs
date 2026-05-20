using System.ComponentModel.DataAnnotations;

namespace Sferity.Backend.DTOs.Requests
{
    public class CreatePromoCodeRequest
    {
        [Range(1, 10000)]
        public int CreditAmount { get; set; }
        [Range(1, 365)]
        public int ExpirationDays { get; set; }
        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}