using System.Text.Json.Serialization;

namespace Sferity.Backend.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PromoCodeStatus { Pending, Active, Used, Expired, Disabled }

    public class PromoCode
    {
        public int Id { get; set; }
        public Guid Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ActiveFrom { get; set; } 
        public DateTime ExpiresAt { get; set; }
        public PromoCodeStatus Status { get; set; } = PromoCodeStatus.Active;
        public int CreditAmount { get; set; }
    }
}