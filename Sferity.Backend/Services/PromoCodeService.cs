using Microsoft.EntityFrameworkCore;
using QRCoder;
using Sferity.Backend.Extensions;
using Sferity.Backend.Data;
using Sferity.Backend.DTOs;
using Sferity.Backend.DTOs.Requests;
using Sferity.Backend.Models;

namespace Sferity.Backend.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly AppDbContext _db;
        private readonly IPolandTimeService _time;

        public PromoCodeService(AppDbContext db, IPolandTimeService time)
        {
            _db = db;
            _time = time;
        }

        public async Task<IEnumerable<PromoCodeDto>> GenerateAsync(CreatePromoCodeRequest request)
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(_time.Now());
            
            var requestedActiveFrom = request.ActiveFrom ?? today;
            
            var activeFrom = requestedActiveFrom == today ? now : _time.EndOfPreviousDayUtc(requestedActiveFrom);
            
            var expiresAt = request.ExpiresOn.HasValue
                ? _time.EndOfDayUtc(request.ExpiresOn.Value)
                : _time.EndOfDayUtc(today.AddDays(request.ExpirationDays!.Value));
            
            var status = activeFrom > now ? PromoCodeStatus.Pending : PromoCodeStatus.Active;
            
            var promos = Enumerable.Range(0, request.Quantity).Select(_ => new PromoCode
            {
                Code = Guid.NewGuid(),
                Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
                AllowLabelRedemption = !string.IsNullOrWhiteSpace(request.Label) && request.AllowLabelRedemption,
                CreatedAt = now,
                ActiveFrom = activeFrom,
                ExpiresAt = expiresAt,
                Status = status,
                CreditAmount = request.CreditAmount
            }).ToList();

            _db.PromoCodes.AddRange(promos);
            await _db.SaveChangesAsync();

            return promos.Select(p => p.ToDto());
        }

        public async Task<PromoCodeDto?> PreviewAsync(PromoCodeIdentifierRequest request)
        {
            PromoCode? promo = null;

            if (request.Code.HasValue)
            {
                promo = await _db.PromoCodes
                    .FirstOrDefaultAsync(x => x.Code == request.Code.Value && x.Status == PromoCodeStatus.Active);
            }
            else if (!string.IsNullOrWhiteSpace(request.Label))
            {
                promo = await _db.PromoCodes
                    .Where(x => x.Label == request.Label.Trim().ToUpper() && x.AllowLabelRedemption && x.Status == PromoCodeStatus.Active)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (promo == null || _time.IsExpired(promo.ExpiresAt))
                return null;

            return promo.ToDto();
        }
        
        public async Task<PromoCodeDto?> RedeemAsync(PromoCodeIdentifierRequest request)
        {
            PromoCode? promo = null;

            if (request.Code.HasValue)
            {
                promo = await _db.PromoCodes
                    .FirstOrDefaultAsync(x => x.Code == request.Code.Value && x.Status == PromoCodeStatus.Active);
            }
            else if (!string.IsNullOrWhiteSpace(request.Label))
            {
                promo = await _db.PromoCodes
                    .Where(x => x.Label == request.Label.Trim().ToUpper() && x.AllowLabelRedemption && x.Status == PromoCodeStatus.Active)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (promo == null)
                return null;

            if (_time.IsExpired(promo.ExpiresAt))
                return null;
            
            promo.Status = PromoCodeStatus.Used;
            await _db.SaveChangesAsync();

            return promo.ToDto();
        }
        
        public async Task<IEnumerable<PromoCodeDto>> GetByIdentifierAsync(PromoCodeIdentifierRequest request)
        {
            if (request.Code.HasValue)
            {
                var promo = await _db.PromoCodes.FirstOrDefaultAsync(x => x.Code == request.Code.Value);

                if (promo == null)
                    return [];
                else
                    return [promo.ToDto()];
            }

            if (!string.IsNullOrWhiteSpace(request.Label))
            {
                var promos = await _db.PromoCodes
                    .Where(x => x.Label == request.Label.Trim().ToUpper())
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                return promos.Select(p => p.ToDto());
            }

            return [];
        }

        public async Task<IEnumerable<PromoCodeDto>> GetAllAsync()
        {
            var promos = await _db.PromoCodes.ToListAsync();
            return promos.Select(p => p.ToDto());
        }
        
        public async Task<int> ExpirePromoCodesAsync()
        {
            var now = DateTime.UtcNow;
            return await _db.PromoCodes
                .Where(p => p.Status == PromoCodeStatus.Active && p.ExpiresAt < now)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PromoCodeStatus.Expired));
        }
        
        public async Task<int> ActivatePendingCodesAsync()
        {
            var now = DateTime.UtcNow;
            return await _db.PromoCodes
                .Where(p => p.Status == PromoCodeStatus.Pending && p.ActiveFrom <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PromoCodeStatus.Active));
        }
        
        public async Task<UpdateDataResultDto> DisableAsync(DisablePromoCodesRequest request)
        {
            var query = _db.PromoCodes.Where(p => p.Status == PromoCodeStatus.Active || p.Status == PromoCodeStatus.Pending);

            if (request.Codes is { Count: > 0 })
                query = query.Where(p => request.Codes.Contains(p.Code));

            if (!string.IsNullOrWhiteSpace(request.Label))
                query = query.Where(p => p.Label == request.Label.Trim().ToUpper());

            if (request.CreditAmount.HasValue)
                query = query.Where(p => p.CreditAmount == request.CreditAmount.Value);

            if (request.CreatedFrom.HasValue)
                query = query.Where(p => p.CreatedAt >= request.CreatedFrom.Value);

            if (request.CreatedTo.HasValue)
                query = query.Where(p => p.CreatedAt <= request.CreatedTo.Value);

            if (request.ExpiresFrom.HasValue)
                query = query.Where(p => p.ExpiresAt >= request.ExpiresFrom.Value);

            if (request.ExpiresTo.HasValue)
                query = query.Where(p => p.ExpiresAt <= request.ExpiresTo.Value);

            var count = await query
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PromoCodeStatus.Disabled));

            return new UpdateDataResultDto
            {
                UpdatedCount = count,
                Message = $"{count} code{(count == 1 ? "" : "s")} disabled successfully."
            };
        }
        
        public async Task<UpdateDataResultDto> UpdateAsync(UpdatePromoCodeRequest request)
        {
            var promos = await _db.PromoCodes.Where(x => request.Codes.Contains(x.Code)).ToListAsync();

            if (promos.Count == 0)
                return new UpdateDataResultDto
                {
                    UpdatedCount = 0,
                    Message = "No codes matched the provided GUIDs."
                };
            
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(_time.Now());
            
            foreach (var promo in promos)   
            {
                if (request.Label != null)
                {
                    promo.Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim().ToUpper();

                    if (promo.Label == null)
                        promo.AllowLabelRedemption = false;
                }

                var finalLabel = request.Label ?? promo.Label;

                if (request.AllowLabelRedemption.HasValue && !string.IsNullOrWhiteSpace(finalLabel))
                {
                    promo.AllowLabelRedemption = request.AllowLabelRedemption.Value;
                }

                if (request.CreditAmount.HasValue)
                    promo.CreditAmount = request.CreditAmount.Value;

                if (request.ActiveFrom.HasValue)
                {
                    promo.ActiveFrom = request.ActiveFrom.Value == today
                        ? now
                        : _time.EndOfPreviousDayUtc(request.ActiveFrom.Value);

                    if (promo.Status == PromoCodeStatus.Active || promo.Status == PromoCodeStatus.Pending)
                        promo.Status = promo.ActiveFrom > now ? PromoCodeStatus.Pending : PromoCodeStatus.Active;
                }
                
                if (request.ExpiresOn.HasValue)
                {
                    var newExpiresAt = _time.EndOfDayUtc(request.ExpiresOn.Value);
                    promo.ExpiresAt = newExpiresAt;

                    if (promo.Status == PromoCodeStatus.Expired && newExpiresAt >= _time.Now())
                        promo.Status = promo.ActiveFrom > now ? PromoCodeStatus.Pending : PromoCodeStatus.Active;
                }

                if (request.Status.HasValue)
                {
                    var newStatus = request.Status.Value;
                    
                    var validTransition =
                        (promo.Status == PromoCodeStatus.Pending && newStatus == PromoCodeStatus.Active) ||
                        (promo.Status == PromoCodeStatus.Pending && newStatus == PromoCodeStatus.Disabled) ||
                        (promo.Status == PromoCodeStatus.Active && newStatus == PromoCodeStatus.Active) ||
                        (promo.Status == PromoCodeStatus.Active && newStatus == PromoCodeStatus.Pending) ||
                        (promo.Status == PromoCodeStatus.Active && newStatus == PromoCodeStatus.Disabled) ||
                        (promo.Status == PromoCodeStatus.Disabled && newStatus == PromoCodeStatus.Active) ||
                        (promo.Status == PromoCodeStatus.Disabled && newStatus == PromoCodeStatus.Pending) ||
                        (promo.Status == PromoCodeStatus.Used && newStatus == PromoCodeStatus.Active) ||
                        (promo.Status == PromoCodeStatus.Expired && newStatus == PromoCodeStatus.Active) ||
                        (promo.Status == PromoCodeStatus.Expired && newStatus == PromoCodeStatus.Pending);
                    
                    if (validTransition)
                        promo.Status = newStatus;
                }
            }

            await _db.SaveChangesAsync();

            return new UpdateDataResultDto
            {
                UpdatedCount = promos.Count,
                Message = $"{promos.Count} code{(promos.Count == 1 ? "" : "s")} updated successfully."
            };
        }
        
        public async Task<UpdateDataResultDto> DeleteAsync(DeletePromoCodesRequest request)
        {
            var count = await _db.PromoCodes
                .Where(x => request.Codes.Contains(x.Code))
                .ExecuteDeleteAsync();

            return new UpdateDataResultDto
            {
                UpdatedCount = count,
                Message = count == 0
                    ? "No codes matched the provided GUIDs."
                    : $"{count} code{(count == 1 ? "" : "s")} deleted successfully."
            };
        }
        
        public async Task<string?> GetQrCodeSvgAsync(Guid code)
        {
            var exists = await _db.PromoCodes.AnyAsync(x => x.Code == code);

            if (!exists)
                return null;

            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(code.ToString(), QRCodeGenerator.ECCLevel.M);
            var qrCode = new SvgQRCode(qrData);

            return qrCode.GetGraphic(5, "#000000", "#ffffff");
        }
    }
}