namespace Sferity.Backend.DTOs.Requests;

public class PromoCodeIdentifierRequest
{
    public Guid? Code { get; set; }
    public string? Label { get; set; }
}