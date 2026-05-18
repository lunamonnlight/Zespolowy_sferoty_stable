using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Sferity.Backend.PdfDocuments;

public class KRSReportDocument
{
    private readonly string _companyName;
    private readonly string _nip;
    private readonly string _aiReport;

    public KRSReportDocument(string companyName, string nip, string aiReport)
    {
        _companyName = companyName;
        _nip = nip;
        _aiReport = aiReport;
    }

    public byte[] GeneratePdf()
    {
        return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("Raport Analityczny")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Text($"Firma: {_companyName}").FontSize(14).SemiBold();
                        col.Item().Text($"NIP: {_nip}").FontSize(12).FontColor(Colors.Grey.Medium);
                    
                        col.Item().PaddingTop(20).Text("Ocena i analiza ryzyka:").Bold().FontSize(14);
                    
                        // Wrzucamy gładki, oczyszczony tekst AI
                        col.Item().PaddingTop(5).Text(_aiReport).FontSize(11);
                    });
                });
            })
            .GeneratePdf();
    }
}