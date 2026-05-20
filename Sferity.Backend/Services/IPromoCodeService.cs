using Sferity.Backend.DTOs;
using Sferity.Backend.DTOs.Requests;

namespace Sferity.Backend.Services
{
    public interface IPromoCodeService
    {
        Task<IEnumerable<PromoCodeDto>> GenerateAsync(CreatePromoCodeRequest request);
        Task<PromoCodeDto?> PreviewAsync(PromoCodeIdentifierRequest request);
        Task<PromoCodeDto?> RedeemAsync(PromoCodeIdentifierRequest request, int userId); 
        Task<IEnumerable<PromoCodeDto>> GetByIdentifierAsync(PromoCodeIdentifierRequest request);
        Task<IEnumerable<PromoCodeDto>> GetAllAsync();
        Task<int> ExpirePromoCodesAsync();
        Task<int> ActivatePendingCodesAsync();
        Task<UpdateDataResultDto> DisableAsync(DisablePromoCodesRequest request);
        Task<UpdateDataResultDto> UpdateAsync(UpdatePromoCodeRequest request);
        Task<UpdateDataResultDto> DeleteAsync(DeletePromoCodesRequest request);
        Task<string?> GetQrCodeSvgAsync(Guid code);
    }
}