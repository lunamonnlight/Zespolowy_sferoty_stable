using System.ComponentModel.DataAnnotations;

namespace Sferity.Backend.DTOs.Requests
{
    public class CreatePromoCodeRequest : IValidatableObject
    {
        [Range(1, 10000)]
        public int CreditAmount { get; set; }

        // Expiration must be defined either by duration (ExpirationDays)
        // or by a specific date (ExpiresOn), but not both
        [Range(0, 365)]
        public int? ExpirationDays { get; set; }
        public DateOnly? ExpiresOn { get; set; }
        // If not provided, defaults to date of creation
        public DateOnly? ActiveFrom { get; set; }
        [MaxLength(100)]
        public string? Label { get; set; }
        public bool AllowLabelRedemption { get; set; } = false;
        // Number of codes generated with identical parameters
        [Range(1, 10000)]
        public int Quantity { get; set; } = 1;
        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpirationDays == null && ExpiresOn == null)
                yield return new ValidationResult(
                    "Either ExpirationDays or ExpiresOn must be provided.",
                    [nameof(ExpirationDays), nameof(ExpiresOn)]);

            if (ExpirationDays != null && ExpiresOn != null)
                yield return new ValidationResult(
                    "Provide either ExpirationDays or ExpiresOn, not both.",
                    [nameof(ExpirationDays), nameof(ExpiresOn)]);

            if (ExpiresOn != null && ExpiresOn < DateOnly.FromDateTime(DateTime.Today))
                yield return new ValidationResult(
                    "ExpiresOn cannot be in the past.",
                    [nameof(ExpiresOn)]);
            
            if (!string.IsNullOrWhiteSpace(Label) && Label != Label.ToUpper())
                yield return new ValidationResult(
                    "Label must be uppercase.",
                    [nameof(Label)]);
            
            var effectiveActiveFrom = ActiveFrom ?? DateOnly.FromDateTime(DateTime.Today);
            var effectiveExpiresOn = ExpiresOn ?? DateOnly.FromDateTime(DateTime.Today.AddDays(ExpirationDays ?? 0));

            if (effectiveActiveFrom >= effectiveExpiresOn)
                yield return new ValidationResult(
                    "ActiveFrom must be before the expiry date.",
                    [nameof(ActiveFrom)]);
        }
    }
}