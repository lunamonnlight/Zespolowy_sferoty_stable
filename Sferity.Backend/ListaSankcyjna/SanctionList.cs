using ClosedXML.Excel;
using Sferity.Backend.Models;

namespace Sferity.Backend.ListaSankcyjna;
using Backend.Models;
using ClosedXML.Excel.CalcEngine;
using ClosedXML.Graphics;

// GLOWNE FUNKCJE DLA LISTY SANKCYJNEJ
public class SanctionList
{
    private static List<SanctionedEntity> _fullSanctionList = new List<SanctionedEntity>();

    public async Task<List<SanctionedEntity>> LoadFullSanctionListAsync(string nazwa)
    {
        // POBIERANIE PLIKU
        var url = "https://www.gow.pl/attachment/a90f30ee-3e5d-402b-994e-fed11909d3d2";

        using var http = new HttpClient();
        var response = await http.GetByteArrayAsync(url);

        using var stream = new MemoryStream(response);
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("podmioty");

        var result = new List<SanctionedEntity>();
        
        // WYSZUKIWANIE PO NAZWIE POLA
        var headerRow = sheet.FirstRowUsed();

        var nameColumn = headerRow?.CellsUsed()
            .FirstOrDefault(c => c.GetString() == "Nazwa podmiotu");

        var descriptionColumn = headerRow?.CellsUsed()
            .FirstOrDefault(c => c.GetString() == "Dane identyfikacyjne podmiotu");

        var reasonColumn = headerRow?.CellsUsed()
            .FirstOrDefault(c => c.GetString() == "Uzasadnienie wpisu na listę");

        if (nameColumn == null || descriptionColumn == null || reasonColumn == null)
            throw new Exception("Nie znaleziono wymaganych kolumn");

        int nameIndex = nameColumn.Address.ColumnNumber;
        int descriptionIndex = descriptionColumn.Address.ColumnNumber;
        int reasonIndex = reasonColumn.Address.ColumnNumber;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var name = row.Cell(nameIndex).GetString();
            var description = row.Cell(descriptionIndex).GetString();
            var reason = row.Cell(reasonIndex).GetString();

            if (!string.IsNullOrWhiteSpace(name) &&
                name.Contains(nazwa, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SanctionedEntity
                {
                    name = name,
                    description = description,
                    reason = reason
                });
            }
        }

        return result;
    }    
    
}