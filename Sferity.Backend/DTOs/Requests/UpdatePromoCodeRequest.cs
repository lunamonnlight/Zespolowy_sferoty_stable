using System.ComponentModel.DataAnnotations;
using Sferity.Backend.Models;

namespace Sferity.Backend.DTOs.Requests;

public class UpdatePromoCodeRequest
{
    public List<Guid> Codes { get; set; } = [];

    [MaxLength(100)]
    public string? Label { get; set; }

    public bool? AllowLabelRedemption { get; set; }

    [Range(1, 10000)]
    public int? CreditAmount { get; set; }
    
    public DateOnly? ActiveFrom { get; set; } 

    public DateOnly? ExpiresOn { get; set; }

    public PromoCodeStatus? Status { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Codes.Count == 0)
            yield return new ValidationResult(
                "At least one code must be provided.",
                [nameof(Codes)]);

        if (ExpiresOn != null && ExpiresOn < DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult(
                "ExpiresOn cannot be in the past.",
                [nameof(ExpiresOn)]);
        
        if (!string.IsNullOrWhiteSpace(Label) && Label != Label.ToUpper())
            yield return new ValidationResult(
                "Label must be uppercase.",
                [nameof(Label)]);

        if (ActiveFrom != null && ExpiresOn != null && ActiveFrom >= ExpiresOn)
            yield return new ValidationResult(
                "ActiveFrom must be before ExpiresOn.",
                [nameof(ActiveFrom)]);
    }
}