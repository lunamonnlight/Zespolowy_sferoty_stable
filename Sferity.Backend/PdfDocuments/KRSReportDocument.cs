using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sferity.Backend.Models;
using Sferity.Backend.ListaSankcyjna;

// RAPORT KONTRAHANTERA PDF
namespace Sferity.Backend.PdfDocuments;

public class KRSReportDocument
{
    private static readonly CultureInfo Pl = new("pl-PL");

    private const string MainBlue = "#2388de";
    private const string LightBlue = "#08349c";


    private enum ReportMode
    {
        Standard,
        Finance,
        Legal,
        Sales
    }

    private enum ValueKind
    {
        Money,
        Percent,
        Number
    }
    
    

    public async Task<byte[]> GenerateReport(KRSReport report, string aiContent, string reportType)
    {
        var listaSankcyjna = new List<SanctionedEntity>();

        if (report.ListaSankcyjna)
        {
            try
            {
                var sanctionService = new SanctionList();

                listaSankcyjna = await sanctionService
                                     .LoadFullSanctionListAsync(report.PodstawoweInformacje?.Nazwy?.Pelna ?? "")
                                 ?? new List<SanctionedEntity>();
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Dane są niedostępne" + ex.Message);

                // fallback
                listaSankcyjna = new List<SanctionedEntity>();
            }
        }
        
        var mode = ParseMode(reportType);

        var years = report.RaportFinansowy?.RaportFinansowy?
            .OrderByDescending(x => x.Rok)
            .Take(3)
            .ToList() ?? new List<RaportFinansowySzczegoly>();

        return Document.Create(container =>
        {
            // Rozmiary strony
            container.Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Verdana));

                page.Header().ShowOnce().Row(row =>
                {
                    
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(GetTitle(mode))
                            .FontSize(18)
                            .Bold()
                            .FontColor(MainBlue);
                        row.Spacing(20);

                        col.Item().Text(report.PodstawoweInformacje?.Nazwy?.Pelna ?? "b/d")
                            .FontSize(7)
                            .SemiBold();
                        col.Spacing(10);
                    });
                    row.Spacing(20);

                    row.ConstantItem(130).AlignRight().Column(col =>
                    {
                        col.Item().Text(DateTime.Now.ToString("d", Pl)).AlignRight();
                        col.Item().Text($"Typ: {reportType}").FontSize(8).AlignRight();
                    });
                    
                    
                });
                
                // Kolejnosc wysiwtlania
                page.Content().Column(col =>
                {
                    col.Spacing(60);
                    
                    
                    AddIdentitySection(col, report, mode);
                    if (mode == ReportMode.Finance)
                    {
                        AddFinancialSection(col, years, mode);
                    }

                    else if (mode == ReportMode.Legal)
                    {
                        AddSanctionSection(col, listaSankcyjna);
                    }

                    else if (mode == ReportMode.Sales)
                    {
                        AddFinancialSection(col, years, mode);
                    }
                    else
                    {
                        if (report.RaportFinansowy?.RaportFinansowy?
                                .Any(x => x != null && (
                                    x.AktywaNetto != 0 ||
                                    x.Przychody != 0 ||
                                    x.Zyski?.ZyskOgolem != 0
                                )) == true)
                        {
                            AddFinancialSection(col, years, mode);
                        }

                        if (report.ListaSankcyjna == true)
                        {
                            AddSanctionSection(col, listaSankcyjna);
                        }
                    }
                    
                    

                    col.Item().Element(CellStyleHeaderSection)
                        .Text("Interpretacja i ocena")
                        .FontSize(14)
                        .Bold()
                        .FontColor(MainBlue);

                    col.Item()
                        .Element(c => RenderMarkdown(c, aiContent));

                    AddDocumentsSection(col, report);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    // sprawdzanie filtru
    private static ReportMode ParseMode(string reportType)
        => reportType.Trim().ToLowerInvariant() switch
        {
            "finance" or "finansista" => ReportMode.Finance,
            "prawnik" => ReportMode.Legal,
            "sales" or "handlowiec" => ReportMode.Sales,
            _ => ReportMode.Standard
        };

    // dodawanie titlu
    private static string GetTitle(ReportMode mode) => mode switch
    {
        ReportMode.Finance => "RAPORT ANALIZY FINANSOWEJ",
        ReportMode.Legal => "RAPORT WERYFIKACJI PRAWNEJ",
        ReportMode.Sales => "RAPORT POTENCJAŁU HANDLOWEGO",
        _ => "RAPORT WERYFIKACJI KONTRAHENTA"
    };

    // dodawanie informacji podstawowych
private void AddIdentitySection(ColumnDescriptor col, KRSReport report, ReportMode mode)
{
    col.Spacing(20);
    col.Item().Element(CellStyleHeaderSection)
        .Text("Informacje podstawowe")
        .FontSize(14)
        .Bold()
        .FontColor(MainBlue);

    col.Item().Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(2);
            columns.RelativeColumn(4);
        });

        void Row(string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "b/d")
                return;

            table.Cell().Element(CellStyle).Text(label).SemiBold();
            table.Cell().Element(CellStyle).Text(value);
        }

        void RowBool(string label, bool? value)
        {
            if (!value.HasValue)
                return;

            table.Cell().Element(CellStyle).Text(label).SemiBold();
            table.Cell().Element(CellStyle).Text(value.Value ? "TAK" : "NIE");
        }

        void RowNumber(string label, object? value)
        {
            if (value == null)
                return;
            
            if (value is int i && i == 0) return;
            if (value is long l && l == 0) return;

            table.Cell().Element(CellStyle).Text(label).SemiBold();
            table.Cell().Element(CellStyle).Text(value.ToString());
        }

        // --- WSPÓLNE ---
        //Row("Pełna nazwa", report.PodstawoweInformacje?.Nazwy?.Pelna);

        if (mode == ReportMode.Finance)
        {
            var stan = report.PodstawoweInformacje?.Stan;

            Row("Status", BuildStatus(stan));
            RowBool("Czy w likwidacji", stan?.WLikwidacji);
            RowBool("Czy w upadłości", stan?.WUpadlosci);
            RowBool("Czy w zawieszeniu", stan?.WZawieszeniu);
            RowBool("Czy dofinansowana przez UE", stan?.CzyDofinansowanaPrzezUe);
            RowBool("Czy otrzymała pomoc publiczną", stan?.CzyOtrzymalaPomocPubliczna);
        }
        else if (mode == ReportMode.Legal)
        {
            var p = report.PodstawoweInformacje;

            Row("Główna osoba", p?.GlownaOsoba?.ImionaINazwisko);
            Row("Adres", FormatAddress(p?.Adres));
            Row("Forma prawna", p?.Stan?.FormaPrawna);
            Row("Wielkość", p?.Stan?.Wielkosc);

            RowNumber("NIP", p?.Numery?.Nip);
            RowNumber("KRS", p?.Numery?.Krs);

            Row("Data wpisu do KRS", p?.KrsRejestry?.RejestrPrzedsiebiorcowDataWpisu);
            Row("Data wykreślenia z KRS", p?.KrsRejestry?.RejestrPrzedsiebiorcowDataWykreslenia);
            Row("Data najnowszego wpisu", p?.KrsWpisy?.NajnowszyData);
            RowNumber("Numer najnowszego wpisu", p?.KrsWpisy?.NajnowszyNumer);
            Row("Pierwszy wpis", p?.KrsWpisy?.PierwszyData);

            RowNumber("Aktualne powiązania", p?.KrsPowiazaniaLiczby?.Aktualne);
            RowNumber("Przeszłe powiązania", p?.KrsPowiazaniaLiczby?.Przeszle);

            Row("Data synchronizacji KRS", p?.Metadane?.KrsOdpisSynchronizacjaDataCzas);

            var rozdzialy = JoinOrBd(p?.Metadane?.KrsRozdzialyDostepne);
            if (rozdzialy != "b/d") Row("Dostępne rozdziały KRS", rozdzialy);

            RowNumber("REGON", p?.Numery?.Regon);
            RowNumber("DUNS", p?.Numery?.Duns);
        }
        else if (mode == ReportMode.Sales)
        {
            var p = report.PodstawoweInformacje;

            Row("Główna osoba", p?.GlownaOsoba?.ImionaINazwisko);
            Row("WWW", p?.Kontakt?.Www);

            var emails = JoinOrBd(p?.Kontakt?.Emaile);
            if (emails != "b/d") Row("E-mail", emails);
        }
        else
        {
            var p = report.PodstawoweInformacje;
    var stan = p?.Stan;

    // NAZWY
    if (p?.Nazwy?.Pelna != null)
        Row("Pełna nazwa", p.Nazwy.Pelna);

    if (p?.Nazwy?.Skrocona != null)
        Row("Skrócona nazwa", p.Nazwy.Skrocona);

    // OSOBA
    if (p?.GlownaOsoba?.ImionaINazwisko != null)
        Row("Główna osoba", p.GlownaOsoba.ImionaINazwisko);

    // ADRES
    var adres = FormatAddress(p?.Adres);
    if (adres != "b/d")
        Row("Adres", adres);

    // KONTAKT
    if (!string.IsNullOrWhiteSpace(p?.Kontakt?.Www))
        Row("WWW", p.Kontakt.Www);

    var emails = JoinOrBd(p?.Kontakt?.Emaile);
    if (emails != "b/d")
        Row("E-mail", emails);

    // NUMERY
    if (p?.Numery?.Nip != null)
        RowNumber("NIP", p.Numery.Nip);

    if (p?.Numery?.Krs != null)
        RowNumber("KRS", p.Numery.Krs);

    if (p?.Numery?.Regon != null)
        RowNumber("REGON", p.Numery.Regon);

    if (p?.Numery?.Duns != null)
        RowNumber("DUNS", p.Numery.Duns);

    // STAN
    if (stan != null)
    {
        Row("Status", BuildStatus(stan));
        RowBool("Czy w likwidacji", stan.WLikwidacji);
        RowBool("Czy w upadłości", stan.WUpadlosci);
        RowBool("Czy w zawieszeniu", stan.WZawieszeniu);
        RowBool("Czy dofinansowana przez UE", stan.CzyDofinansowanaPrzezUe);
        RowBool("Czy otrzymała pomoc publiczną", stan.CzyOtrzymalaPomocPubliczna);

        if (!string.IsNullOrWhiteSpace(stan.FormaPrawna))
            Row("Forma prawna", stan.FormaPrawna);

        if (!string.IsNullOrWhiteSpace(stan.Wielkosc))
            Row("Wielkość", stan.Wielkosc);

        if (!string.IsNullOrWhiteSpace(stan.PkdPrzewazajaceDzial))
            Row("PKD przeważające", stan.PkdPrzewazajaceDzial);
    }

    // KRS
    if (p?.KrsRejestry?.RejestrPrzedsiebiorcowDataWpisu != null)
        Row("Data wpisu do KRS", p.KrsRejestry.RejestrPrzedsiebiorcowDataWpisu);

    if (p?.KrsWpisy?.NajnowszyData != null)
        Row("Data najnowszego wpisu", p.KrsWpisy.NajnowszyData);

    if (p?.KrsWpisy?.NajnowszyNumer != null)
        RowNumber("Numer najnowszego wpisu", p.KrsWpisy.NajnowszyNumer);

    if (p?.KrsPowiazaniaLiczby?.Aktualne != null)
        RowNumber("Aktualne powiązania", p.KrsPowiazaniaLiczby.Aktualne);

    if (p?.Metadane?.KrsOdpisSynchronizacjaDataCzas != null)
        Row("Data synchronizacji KRS", p.Metadane.KrsOdpisSynchronizacjaDataCzas);

    var rozdzialy = JoinOrBd(p?.Metadane?.KrsRozdzialyDostepne);
    if (rozdzialy != "b/d")
        Row("Dostępne rozdziały KRS", rozdzialy);
        }
    });
}

// dodawanie tablicy dla analizy finansowej
private void AddFinancialSection(ColumnDescriptor col, List<RaportFinansowySzczegoly> years, ReportMode mode)
{
    if (years == null || years.Count == 0)
        return;

    bool HasAnyData(Func<RaportFinansowySzczegoly, decimal> selector)
        => years.Any(y => selector(y) != 0);

    col.Item().Element(CellStyleHeaderSection)
        .Text("Analiza finansowa")
        .FontSize(14)
        .Bold()
        .FontColor(MainBlue);

    col.Item().Table(table =>
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3.2f);
            foreach (var _ in years)
                columns.RelativeColumn(1.25f);
            columns.RelativeColumn(1.1f);
        });

        table.Header(header =>
        {
            header.Cell().Element(HeaderStyle).Text("Pozycja").FontColor(Colors.White).Bold().FontSize(10);

            foreach (var year in years)
                header.Cell().Element(HeaderStyle).Text(year.Rok.ToString()).FontColor(Colors.White).Bold().FontSize(10);

            header.Cell().Element(HeaderStyle).Text("Dynamika").FontColor(Colors.White).Bold().FontSize(10);
        });

        int rowIndex = 0;

        void AddRowIfData(string label, Func<RaportFinansowySzczegoly, decimal> selector, ValueKind kind)
        {
            if (!HasAnyData(selector))
                return;

            var alt = rowIndex++ % 2 == 1;
            AddRow(table, label, years, selector, kind, alt);
        }

        AddRowIfData("Aktywa netto", x => x.AktywaNetto, ValueKind.Money);
        AddRowIfData("Przychody", x => x.Przychody, ValueKind.Money);
        AddRowIfData("Zysk operacyjny", x => x.Zyski.ZyskOperacyjny, ValueKind.Money);
        AddRowIfData("Zysk finansowy", x => x.Zyski.ZyskFinansowy, ValueKind.Money);
        AddRowIfData("Zysk ogółem", x => x.Zyski.ZyskOgolem, ValueKind.Money);
        AddRowIfData("Marża netto", x => x.Marze.MarzaNetto, ValueKind.Percent);
        AddRowIfData("Płynność bieżąca", x => x.Plynnosc.PlynnoscBiezaca, ValueKind.Number);
        AddRowIfData("Rotacja zobowiązań (dni)", x => x.Rotacja.RotacjaZobowiazan, ValueKind.Number);
        AddRowIfData("Stan gotówki w kasie", x => x.Stany.StanGotowkiWKasie, ValueKind.Money);
    });
}    private void AddRow( 
    TableDescriptor table,
    string label,
    List<RaportFinansowySzczegoly> years,
    Func<RaportFinansowySzczegoly, decimal> selector,
    ValueKind kind,
    bool alt)
// funkcja dodawania wierszy
{
    table.Cell().Element(c => ValueCellStyle(c, alt)).Text(label).SemiBold();

    for (int i = 0; i < years.Count; i++)
    {
        var value = selector(years[i]);

        table.Cell()
            .Element(c => ValueCellStyle(c, alt))
            .AlignRight()
            .Text(FormatValue(value, kind))
            .FontSize(7);
    }

    var oldest = selector(years.Last());
    var newest = selector(years.First());

    table.Cell()
        .Element(c => ValueCellStyle(c, alt))
        .AlignRight()
        .Text(CalculateDynamic(oldest, newest))
        .SemiBold()
        .FontSize(8);
}
    
    
    
    private static void AddRowIfExists(
        TableDescriptor table,
        string label,
        List<RaportFinansowySzczegoly> years,
        Func<RaportFinansowySzczegoly, decimal?> selector,
        ValueKind kind)
    {
        
        var hasAnyValue = years.Any(y => selector(y).HasValue);

        if (!hasAnyValue)
            return;

        table.Cell().Element(c => CellStyle(c)).Text(label).SemiBold();

        foreach (var year in years)
        {
            var value = selector(year);

            table.Cell()
                .Element((Func<IContainer, IContainer>)(c => CellStyle(c)))
                .AlignRight()
                .Text(value.HasValue ? FormatValue(value.Value, kind) : "")
                .FontSize(10);
        }

        var oldest = selector(years.Last());
        var newest = selector(years.First());

        if (oldest.HasValue && newest.HasValue && oldest != 0)
        {
            table.Cell()
                .Element(CellStyle)
                .AlignRight()
                .Text(CalculateDynamic(oldest.Value, newest.Value))
                .SemiBold()
                .FontSize(10);
        }
        else
        {
            table.Cell().Element(CellStyle).Text("");
        }
    }
    
    // dodawanie zrodel danych
    private void AddDocumentsSection(ColumnDescriptor col, KRSReport report)
    {
        var docs = report.RaportFinansowy?.ListaDokumentow;
        if (docs == null || docs.Count == 0)
            return;

        col.Item().Element(CellStyleHeaderSection)
            .Text("Dokumenty źródłowe")
            .FontSize(14)
            .Bold()
            .FontColor(MainBlue);

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Rok").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderStyle).Text("Nazwa").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderStyle).Text("JSON").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderStyle).Text("ID").FontColor(Colors.White).Bold();
            });

            for (int i = 0; i < docs.Count; i++)
            {
                var d = docs[i];
                var alt = i % 2 == 1;

                table.Cell().Element(c => ValueCellStyle(c, alt)).Text(d.Year.ToString());
                table.Cell().Element(c => ValueCellStyle(c, alt)).Text(string.IsNullOrWhiteSpace(d.Nazwa) ? "b/d" : d.Nazwa);
                table.Cell().Element(c => ValueCellStyle(c, alt)).Text(d.CzyMaJson ? "TAK" : "NIE");
                table.Cell().Element(c => ValueCellStyle(c, alt)).Text(d.Id.ToString());
            }
        });
    }
    
    // dodawanie listy sankcyjnej
    private void AddSanctionSection(ColumnDescriptor col, List<SanctionedEntity> lista)
    {
        col.Item().Element(CellStyleHeaderSection)
            .Text("Lista sankcyjna")
            .FontSize(14)
            .Bold()
            .FontColor(MainBlue);

        if (lista == null || lista.Count == 0)
        {
            col.Item().Text("Podmiot NIE znajduje się na liście sankcyjnej").FontSize(10);
            return;
        }

        col.Item().Text("Podmiot znajduje się na liście sankcyjnej!")
            .FontSize(10)
            .Bold()
            .FontColor(Colors.Red.Medium);
        

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(3);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Opis").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderStyle).Text("Powód").FontColor(Colors.White).Bold();
            });

            foreach (var item in lista)
            {
                table.Cell().Element(CellStyle).Text(item.description);
                table.Cell().Element(CellStyle).Text(item.reason);
            }
        });
    }


    // renderowanie promtu od AI
    private void RenderMarkdown(IContainer container, string markdown)
    {
        var lines = (markdown ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace("**", "")
            .Replace("*", "")
            .Split('\n')
            .ToList();

        container.Column(col =>
        {
            col.Spacing(5);

            var paragraph = new StringBuilder();

            void FlushParagraph()
            {
                var text = paragraph.ToString().Trim();
                paragraph.Clear();

                if (!string.IsNullOrWhiteSpace(text))
                    col.Item().Text(text).FontSize(10);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph();
                    continue;
                }

                if (line.StartsWith("# "))
                {
                    FlushParagraph();
                    col.Item().Text(line[2..].Trim()).FontSize(16).Bold().FontColor(MainBlue);
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    FlushParagraph();
                    col.Item().Text(line[3..].Trim()).FontSize(13).Bold().FontColor(MainBlue);
                    continue;
                }

                if (line.StartsWith("### "))
                {
                    FlushParagraph();
                    col.Item().Text(line[4..].Trim()).FontSize(11).Bold();
                    continue;
                }

                if (IsMarkdownTableLine(line))
                {
                    FlushParagraph();

                    var tableLines = new List<string> { line };
                    int j = i + 1;

                    while (j < lines.Count && IsMarkdownTableLine(lines[j]))
                    {
                        tableLines.Add(lines[j]);
                        j++;
                    }

                    RenderMarkdownTable(col, tableLines);
                    i = j - 1;
                    continue;
                }

                if (Regex.IsMatch(line, @"^(\-|\*|\d+\.)\s+"))
                {
                    FlushParagraph();
                    var bullet = Regex.Replace(line, @"^(\-|\*|\d+\.)\s+", "• ");
                    col.Item().PaddingLeft(10).Text(bullet);
                    continue;
                }

                if (line.StartsWith(">"))
                {
                    FlushParagraph();
                    col.Item()
                        .BorderLeft(3)
                        .BorderColor(MainBlue)
                        .PaddingLeft(8)
                        .Text(line[1..].Trim())
                        .Italic();
                    continue;
                }

                paragraph.AppendLine(line);
            }

            FlushParagraph();
        });
    }

    // renderowanie tablicy markdown
    private void RenderMarkdownTable(ColumnDescriptor col, List<string> tableLines)
    {
        if (tableLines.Count < 2)
            return;

        var headers = SplitMarkdownRow(tableLines[0]);
        var rows = tableLines
            .Skip(2)
            .Select(SplitMarkdownRow)
            .Where(r => r.Count > 0)
            .ToList();

        if (headers.Count == 0)
            return;

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                    columns.RelativeColumn();
            });

            foreach (var header in headers)
            {
                table.Cell().Element(HeaderStyle).Text(header).FontColor(Colors.White).Bold();
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var alt = i % 2 == 1;

                for (int c = 0; c < headers.Count; c++)
                {
                    var value = c < row.Count ? row[c] : "";
                    table.Cell().Element(x => ValueCellStyle(x, alt)).Text(string.IsNullOrWhiteSpace(value) ? "b/d" : value);
                }
            }
        });
    }

    private static bool IsMarkdownTableLine(string line)
        => line.Contains('|') && line.Trim().Length > 0;

    private static List<string> SplitMarkdownRow(string line)
    {
        return line.Trim()
            .Trim('|')
            .Split('|')
            .Select(x => x.Trim())
            .ToList();
    }

    // formatowanie liczb
    private static string FormatValue(decimal value, ValueKind kind)
    {
        return kind switch
        {
            ValueKind.Money => value.ToString("N2", Pl) + " PLN",
            ValueKind.Percent => value.ToString("N2", Pl) + "%",
            _ => value.ToString("N2", Pl)
        };
    }

    private static string CalculateDynamic(decimal oldest, decimal newest)
    {
        if (oldest == 0)
            return "b/d";

        var change = ((newest - oldest) / Math.Abs(oldest)) * 100m;
        return (change > 0 ? "+" : "") + change.ToString("N1", Pl) + "%";
    }

    private static string BuildStatus(Stan? stan)
    {
        if (stan == null) return "b/d";
        if (stan.WLikwidacji) return "W LIKWIDACJI";
        if (stan.WUpadlosci) return "W UPADŁOŚCI";
        if (stan.WZawieszeniu) return "W ZAWIESZENIU";
        return "AKTYWNA";
    }

    private static string BoolText(bool? value)
        => value.HasValue ? (value.Value ? "TAK" : "NIE") : "b/d";

    private static string JoinOrBd(IEnumerable<string>? values)
    {
        var list = values?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        return list.Count == 0 ? "b/d" : string.Join(", ", list);
    }

    // formatowanie adresu
    private static string FormatAddress(Adres? adres)
    {
        if (adres is null)
            return "b/d";

        var parts = new[]
        {
            adres.Ulica,
            adres.NumerDomu,
            adres.NumerMieszkania,
            adres.Kod,
            adres.Miejscowosc,
            adres.Poczta,
            adres.Panstwo
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var result = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(result) ? "b/d" : result;
    }

    // stylizacja wierszy
    private static IContainer CellStyle(IContainer container) =>
        container
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);

    // stylizacja naglowkow
    private IContainer HeaderStyle(IContainer container) =>
        container
            .Background(MainBlue)
            .PaddingVertical(6)
            .PaddingHorizontal(5);

    private IContainer ValueCellStyle(IContainer container, bool alt) =>
        container
            .Background(alt ? Colors.Grey.Lighten3 : Colors.White)
            .PaddingVertical(5)
            .PaddingHorizontal(5)
            .BorderColor(Colors.Grey.Lighten3);

    private IContainer CellStyleHeaderSection(IContainer container) =>
        container.BorderBottom(2).BorderColor(MainBlue).PaddingVertical(5).PaddingTop(40);
    
}