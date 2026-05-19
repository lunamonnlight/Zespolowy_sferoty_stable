using Microsoft.AspNetCore.Mvc;
using Sferity.Backend.Services;
using Sferity.Backend.DTOs;
using Sferity.Backend.DTOs.Requests;

namespace Sferity.Backend.Controllers;

[ApiController]
[Route("/api/promocodes")]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _service;
    private readonly PromoCodeExpiryService _expiryService;

    public PromoCodesController(IPromoCodeService service,  PromoCodeExpiryService expiryService)
    {
        _service = service;
        _expiryService = expiryService;
    }

    // Generates promo codes based on CreatePromoCodeRequest
    [HttpPost("generate")]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate([FromBody] CreatePromoCodeRequest request)
    {
        var result = await _service.GenerateAsync(request);
        return Ok(result);
    }

    // Accepts either GUID or label as query params
    // Returns a single code after checking if it's valid for viewing purposes
    [HttpGet("preview")]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview([FromQuery] PromoCodeIdentifierRequest request)
    {
        if (request.Code == null && string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Either a code or a label must be provided.");

        if (request.Code != null && !string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Provide either a code or a label, not both.");

        var result = await _service.PreviewAsync(request);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
    
    // Accepts either GUID or label in request body
    //Returns a single valid code for the purpose of extracting its CreditAmount and adding it to a user's balance
    [HttpPost("redeem")]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Redeem([FromBody] PromoCodeIdentifierRequest request)
    {
        if (request.Code == null && string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Either a code or a label must be provided.");
        
        if (request.Code != null && !string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Provide either a code or a label, not both.");

        var result = await _service.RedeemAsync(request);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
    
    // Accepts either GUID or label as query params
    // Returns all matching codes — for admin use
    [HttpGet("lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Lookup([FromQuery] PromoCodeIdentifierRequest request)
    {
        if (request.Code == null && string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Either a code or a label must be provided.");

        if (request.Code != null && !string.IsNullOrWhiteSpace(request.Label))
            return BadRequest("Provide either a code or a label, not both.");

        var result = await _service.GetByIdentifierAsync(request);
        return Ok(result);
    }

    // Returns all promo codes without filtering - for admin use
    [HttpGet]
    [ProducesResponseType(typeof(PromoCodeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    // Allows to run expiration of all codes where current date is past the allowed expiration date,
    // instead of waiting for the background service that activates at 00:00 utc
    [HttpPost("expire-now")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExpireNow()
    {
        await _expiryService.RunExpiryCheckAsync();
        return Ok("Expiry check completed.");
    }

    // Only active codes can be disabled
    // Disables codes based on filters (service enforces Active-only constraint) - for admin use
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable([FromBody] DisablePromoCodesRequest request)
    {
        // Prevent mass update with no filters (safety guard)
        var hasAnyFilter =
            request.Codes is { Count: > 0 } ||
            !string.IsNullOrWhiteSpace(request.Label) ||
            request.CreditAmount.HasValue ||
            request.CreatedFrom.HasValue ||
            request.CreatedTo.HasValue ||
            request.ExpiresFrom.HasValue ||
            request.ExpiresTo.HasValue;

        if (!hasAnyFilter)
            return BadRequest("At least one filter must be provided.");

        var result = await _service.DisableAsync(request);
        return Ok(result);
    }
    
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] UpdatePromoCodeRequest request)
    {
        var result = await _service.UpdateAsync(request);
        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromBody] DeletePromoCodesRequest request)
    {
        var result = await _service.DeleteAsync(request);
        return Ok(result);
    }
    
    // Returns an SVG QR code for the given GUID — frontend can render it directly
    [HttpGet("qr/{code}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(Guid code)
    {
        var svg = await _service.GetQrCodeSvgAsync(code);

        if (svg == null)
            return NotFound();

        return Content(svg, "image/svg+xml");
    }
}