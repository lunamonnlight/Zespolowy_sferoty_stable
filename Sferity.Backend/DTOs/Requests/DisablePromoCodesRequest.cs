using Sferity.Backend.Models;

namespace Sferity.Backend.DTOs.Requests;

public class DisablePromoCodesRequest
{
    // Target by specific GUIDs
    public List<Guid>? Codes { get; set; }

    // Target all codes with this label
    public string? Label { get; set; }

    // Target by credit amount
    public int? CreditAmount { get; set; }

    // Target codes created within a time span
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    // Target codes expiring within a time span
    public DateTime? ExpiresFrom { get; set; }
    public DateTime? ExpiresTo { get; set; }
}