using System.ComponentModel.DataAnnotations;

namespace Sferity.Backend.DTOs.Requests;

public class DeletePromoCodesRequest : IValidatableObject
{
    public List<Guid> Codes { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Codes.Count == 0)
            yield return new ValidationResult(
                "At least one code must be provided.",
                [nameof(Codes)]);
    }
}