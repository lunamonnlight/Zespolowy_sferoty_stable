using System.Reflection.Metadata;
using Sferity.Backend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace Sferity.Backend.PdfDocuments;

public class KRSReportDocument
{
    public byte[] GenerateReport(KRSReport report)
    {
        return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("Raport finansowy")
                        .FontSize(20)
                        .Bold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Firma: {report.PodstawoweInformacje?.Nazwy?.Pelna}");

                        foreach (var r in report.RaportFinansowy?.RaportFinansowy ?? [])
                        {
                            col.Item().Text($"Rok: {r.Rok}");
                            col.Item().Text($"Aktywa: {r.AktywaNetto}");
                        }
                    });
                });
            })
            .GeneratePdf();
    }
}