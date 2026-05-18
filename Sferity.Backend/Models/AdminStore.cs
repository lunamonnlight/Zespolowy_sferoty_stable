using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sferity.Backend.Models;

public class AdminStore
{
    public List<AdminUser> Users { get; set; } = new();
    public List<AdminFund> Funds { get; set; } = new();
    public List<AdminLog> Logs { get; set; } = new();
    public List<SearchLog> SearchLogs { get; set; } = new(); // Nowa lista
}

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    
    // NOWE POLE: Domyślnie każdy jest zwykłym użytkownikiem
    public string Role { get; set; } = "user"; 
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AdminFund
{
    public int Id { get; set; }
    public int UserId { get; set; } // KONIECZNA ZMIANA: Powiązanie z użytkownikiem
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PLN";
}

public class AdminLog
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string User { get; set; } = "System";
}

[Table("SearchLogs")]
public class SearchLog
{
    [Key]
    [Column("Id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("UserId")]
    [Required]
    public int UserId { get; set; }

    [Column("Username")]
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Column("SearchedNip")]
    [MaxLength(20)]
    public string? SearchedNip { get; set; }

    [Column("SearchedKrs")]
    [MaxLength(20)]
    public string? SearchedKrs { get; set; }

    [Column("Timestamp")]
    [Required]
    public DateTime SearchTimestamp { get; set; } = DateTime.UtcNow;

    [Column("IsSuccess")]
    public bool? IsSuccess { get; set; }

    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [Column("Cost")]
    [Required]
    public decimal Cost { get; set; } = 0;

    [Column("IsRequest")]
    [Required]
    public bool IsRequest { get; set; } 

    [Column("Gus")] public bool Gus { get; set; }
    [Column("Vat")] public bool Vat { get; set; }
    [Column("Financial")] public bool Financial { get; set; }
    [Column("Status")] public bool Status { get; set; }
    [Column("Connections")] public bool Connections { get; set; }
    [Column("Beneficiaries")] public bool Beneficiaries { get; set; }
    [Column("ReportAI")] public bool ReportAI { get; set; }
    [Column("Patents")] public bool Patents { get; set; }
    [Column("OdpisKrs")] public bool OdpisKrs { get; set; }
}