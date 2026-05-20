using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sferity.Backend.Models;

namespace Sferity.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly string _dbPath = "admin_database.json";
    public AdminController()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"ŚCIEŻKA DO BAZY DANYCH TO: {_dbPath}");
        Console.WriteLine("==================================================");
    }

    private AdminStore LoadDb()
    {
        if (!System.IO.File.Exists(_dbPath))
        {
            var emptyStore = new AdminStore();
            SaveDb(emptyStore);
            return emptyStore;
        }

        var json = System.IO.File.ReadAllText(_dbPath);
        if (string.IsNullOrWhiteSpace(json)) return new AdminStore();

        return JsonSerializer.Deserialize<AdminStore>(json) ?? new AdminStore();
    }

    private void SaveDb(AdminStore store) 
    {
        var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_dbPath, json);
    }

    [HttpGet("data")]
    public IActionResult GetData() => Ok(LoadDb());

    // ZMIANA: Obsługa relacji 1:1 i dodawania środków
    [HttpPost("add-fund")]
    public IActionResult AddFund([FromBody] AdminFund request)
    {
        var db = LoadDb();
        var user = db.Users.FirstOrDefault(u => u.Id == request.UserId);
        if (user == null) return NotFound("Użytkownik nie istnieje");

        // Szukamy czy użytkownik ma już swój fundusz
        var fund = db.Funds.FirstOrDefault(f => f.UserId == request.UserId);
        
        if (fund == null)
        {
            // Jeśli nie ma, tworzymy nowy
            fund = new AdminFund 
            { 
                Id = db.Funds.Any() ? db.Funds.Max(f => f.Id) + 1 : 1, 
                UserId = request.UserId, 
                Name = "Portfel główny", 
                Amount = request.Amount,
                Currency = "PLN"
            };
            db.Funds.Add(fund);
        }
        else
        {
            // Jeśli ma, po prostu zwiększamy saldo
            fund.Amount += request.Amount;
        }

        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"Zwiększono saldo użytkownika {user.Username} o {request.Amount} PLN. Obecny stan: {fund.Amount} PLN" 
        });

        SaveDb(db);
        return Ok(fund);
    }

    // NOWOŚĆ: Odejmowanie środków
    [HttpPost("subtract-fund")]
    public IActionResult SubtractFund([FromBody] AdminFund request)
    {
        var db = LoadDb();
        var user = db.Users.FirstOrDefault(u => u.Id == request.UserId);
        var fund = db.Funds.FirstOrDefault(f => f.UserId == request.UserId);

        if (user == null || fund == null) return NotFound("Użytkownik lub fundusz nie istnieje");

        fund.Amount -= request.Amount;

        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"Odjęto saldo użytkownika {user.Username} o {request.Amount} PLN. Obecny stan: {fund.Amount} PLN" 
        });

        SaveDb(db);
        return Ok(fund);
    }

    [HttpPost("toggle-user/{id}")]
    public IActionResult ToggleUser(int id)
    {
        var db = LoadDb();
        var user = db.Users.FirstOrDefault(u => u.Id == id);
    
        if (user == null) 
        {
            Console.WriteLine($"BŁĄD: Nie znaleziono użytkownika o ID: {id}");
            return NotFound();
        }

        user.IsBlocked = !user.IsBlocked;
    
        // Logowanie akcji
        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"Zmieniono status blokady użytkownika {user.Username} na {user.IsBlocked}" 
        });

        SaveDb(db);
        Console.WriteLine($"SUKCES: Użytkownik {user.Username} (ID: {id}) status blokady: {user.IsBlocked}");
    
        return Ok(user);
    }

    // NOWOŚĆ: Usuwanie użytkownika (tylko jeśli zablokowany)
    [HttpDelete("delete-user/{id}")]
    public IActionResult DeleteUser(int id)
    {
        var db = LoadDb();
        var user = db.Users.FirstOrDefault(u => u.Id == id);

        if (user == null) return NotFound();
        if (!user.IsBlocked) return BadRequest("Można usuwać tylko zablokowanych użytkowników!");

        // Usuwamy użytkownika
        db.Users.Remove(user);
        
        // Usuwamy przypisany do niego fundusz
        db.Funds.RemoveAll(f => f.UserId == id);

        db.Logs.Insert(0, new AdminLog { 
            Timestamp = DateTime.Now, 
            Action = $"TRWAŁE USUNIĘCIE: Użytkownik {user.Username} został usunięty z systemu wraz z funduszami." 
        });

        SaveDb(db);
        return Ok();
    }
    [HttpGet("user-data/{userId}")]
    public IActionResult GetUserData(int userId)
    {
        var db = LoadDb();
    
        // Pobieramy tylko fundusze należące do tego użytkownika
        var fund = db.Funds.FirstOrDefault(f => f.UserId == userId);
    
        // Pobieramy tylko logi wyszukiwania tego użytkownika
        var userSearchLogs = db.SearchLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.SearchTimestamp)
            .ToList();

        return Ok(new {
            balance = fund?.Amount ?? 0,
            currency = fund?.Currency ?? "PLN",
            searchLogs = userSearchLogs
        });
    }
}