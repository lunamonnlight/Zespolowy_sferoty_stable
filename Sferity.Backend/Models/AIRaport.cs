namespace Sferity.Backend.Models;

// KLASA DLA RAPORTU AI GEMINI
public class AIRaport
{
    public int Id { get; set; }
    public long KrsReportId { get; set; }
    public string MarkdownContent { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// KLASA DLA TYPU RAPORTU (prawnik, finansista, itd)
public class ReportRequest
{
    public KRSReport Report { get; set; }
    public string Type { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<string> SelectedItems { get; set; } = new();
}