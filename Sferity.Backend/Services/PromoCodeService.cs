using System.Text.Json;
using Sferity.Backend.Models;
using Sferity.Backend.DTOs;
using Sferity.Backend.DTOs.Requests;
using Sferity.Backend.Extensions;

namespace Sferity.Backend.Services;

public class PromoCodeService : IPromoCodeService
{
    private readonly string _dbPath = "admin_database.json";
    private readonly IPolandTimeService _time;

    public PromoCodeService(IPolandTimeService time)
    {
        _time = time;
    }

    private AdminStore LoadDb()
    {
        if (!File.Exists(_dbPath)) return new AdminStore();
        var json = File.ReadAllText(_dbPath);
        return JsonSerializer.Deserialize<AdminStore>(json) ?? new AdminStore();
    }

    private void SaveDb(AdminStore db)
    {
        var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_dbPath, json);
    }

    public async Task<IEnumerable<PromoCodeDto>> GetAllAsync()
    {
        return LoadDb().PromoCodes.Select(p => p.ToDto());
    }

    public async Task<IEnumerable<PromoCodeDto>> GenerateAsync(CreatePromoCodeRequest request)
    {
        var db = LoadDb();
        var newPromos = new List<PromoCode>();

        for (int i = 0; i < request.Quantity; i++)
        {
            var promo = new PromoCode
            {
                Id = (db.PromoCodes.Count > 0 ? db.PromoCodes.Max(x => x.Id) : 0) + i + 1,
                Code = Guid.NewGuid(),
                Label = request.Label?.ToUpper(),
                CreditAmount = request.CreditAmount,
                CreatedAt = DateTime.UtcNow,
                ActiveFrom = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays ?? 7),
                Status = PromoCodeStatus.Active
            };
            db.PromoCodes.Add(promo);
            newPromos.Add(promo);
        }

        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"Wygenerowano kody ({request.Quantity} szt.) o wartości {request.CreditAmount} PLN" 
        });

        SaveDb(db);
        return newPromos.Select(p => p.ToDto());
    }

    public async Task<PromoCodeDto?> RedeemAsync(PromoCodeIdentifierRequest request, int userId)
    {
        var db = LoadDb();

        // 1. Znajdź kod w bazie JSON
        var promo = db.PromoCodes.FirstOrDefault(x => 
            (request.Code.HasValue && x.Code == request.Code.Value) || 
            (!string.IsNullOrWhiteSpace(request.Label) && x.Label == request.Label.Trim().ToUpper() && x.AllowLabelRedemption)
        );

        if (promo == null || promo.Status != PromoCodeStatus.Active) return null;

        // 2. Znajdź fundusz użytkownika
        var fund = db.Funds.FirstOrDefault(f => f.UserId == userId);
        if (fund == null) {
            fund = new AdminFund { Id = db.Funds.Count + 1, UserId = userId, Name = "Portfel główny", Amount = 0 };
            db.Funds.Add(fund);
        }

        // 3. Dodaj środki i spal kod
        fund.Amount += promo.CreditAmount;
        promo.Status = PromoCodeStatus.Used;

        // 4. Logowanie dla admina
        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"Użytkownik ID:{userId} doładował {promo.CreditAmount} PLN kodem {promo.Label ?? promo.Code.ToString()}" 
        });

        SaveDb(db);
        return promo.ToDto();
    }

    // Realizacja metod interfejsu, żeby nie było błędów kompilacji
    public async Task<int> ExpirePromoCodesAsync() { return 0; }
    public async Task<int> ActivatePendingCodesAsync() { return 0; }
    public async Task<PromoCodeDto?> PreviewAsync(PromoCodeIdentifierRequest r) => (await GetAllAsync()).FirstOrDefault();
    public async Task<IEnumerable<PromoCodeDto>> GetByIdentifierAsync(PromoCodeIdentifierRequest r) => await GetAllAsync();
    public async Task<UpdateDataResultDto> DisableAsync(DisablePromoCodesRequest r) => new();
    public async Task<UpdateDataResultDto> UpdateAsync(UpdatePromoCodeRequest r) => new();
    public async Task<UpdateDataResultDto> DeleteAsync(DeletePromoCodesRequest r) => new();
    public async Task<string?> GetQrCodeSvgAsync(Guid code) => null;
}