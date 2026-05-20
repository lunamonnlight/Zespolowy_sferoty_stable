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
                Id = db.PromoCodes.Count + 1,
                Code = Guid.NewGuid(), // Tylko to nas interesuje
                CreditAmount = request.CreditAmount,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays),
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

        // 1. Znajdź kod w bazie JSON (szukamy TYLKO po unikalnym GUID)
        // Zmienna 'request.Code' musi zostać przekazana z frontendu
        var promo = db.PromoCodes.FirstOrDefault(x => x.Code == request.Code && x.Status == PromoCodeStatus.Active);

        // Walidacja: czy kod istnieje i czy nie wygasł
        if (promo == null || _time.IsExpired(promo.ExpiresAt)) 
            return null;
    
        // 2. Znajdź fundusz użytkownika (lub stwórz go, jeśli nie istnieje)
        var fund = db.Funds.FirstOrDefault(f => f.UserId == userId);
        if (fund == null) 
        {
            fund = new AdminFund 
            { 
                Id = db.Funds.Any() ? db.Funds.Max(f => f.Id) + 1 : 1, 
                UserId = userId, 
                Name = "Portfel główny", 
                Amount = 0,
                Currency = "PLN"
            };
            db.Funds.Add(fund);
        }

        // 3. DODAJ ŚRODKI I OZNACZ KOD JAKO ZUŻYTY
        fund.Amount += promo.CreditAmount;
        promo.Status = PromoCodeStatus.Used;

        // Pobieramy username dla ładniejszego logu (opcjonalnie)
        var user = db.Users.FirstOrDefault(u => u.Id == userId);
        string name = user?.Username ?? $"ID:{userId}";

        // 4. Logowanie dla admina (usunięto wzmianki o Label)
        db.Logs.Insert(0, new AdminLog 
        {
            Timestamp = DateTime.Now,
            Action = $"DOŁADOWANIE: {name} użył kodu {promo.Code} (+{promo.CreditAmount} PLN). Nowe saldo: {fund.Amount} PLN",
            User = "System"
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
