namespace Sferity.Backend.Models;

public class AIRaport
{
    public int Id { get; set; }
    public int KrsReportId { get; set; }
    public string MarkdownContent { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}