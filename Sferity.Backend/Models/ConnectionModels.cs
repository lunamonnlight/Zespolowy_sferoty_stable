using System.Text.Json.Serialization;

namespace Sferity.Backend.Models;

// ── Wspólne ───────────────────────────────────────────────────────────────────

public class PowiazanieKwerendowane
{
    [JsonPropertyName("dataStart")]
    public string? DataStart { get; set; }

    [JsonPropertyName("dataKoniec")]
    public string? DataKoniec { get; set; }

    [JsonPropertyName("kierunek")]
    public string? Kierunek { get; set; }

    /// <summary>Rola np. "PREZES ZARZĄDU", "CZŁONEK ZARZĄDU". Pusty string gdy brak.</summary>
    [JsonPropertyName("opis")]
    public string? Opis { get; set; }

    /// <summary>KRS_BOARD | KRS_SHAREHOLDER | KRS_FOUNDER | KRS_PROXY</summary>
    [JsonPropertyName("typ")]
    public string? Typ { get; set; }
}

public class PowiazaniaLiczby
{
    [JsonPropertyName("aktualne")]
    public int Aktualne { get; set; }

    [JsonPropertyName("aktualneOrganizacje")]
    public int? AktualneOrganizacje { get; set; }

    [JsonPropertyName("przeszle")]
    public int Przeszle { get; set; }
}

// ── Tożsamość osoby ───────────────────────────────────────────────────────────

public class Tozsamosc
{
    [JsonPropertyName("dataUrodzenia")]
    public string? DataUrodzenia { get; set; }

    [JsonPropertyName("imie")]
    public string? Imie { get; set; }

    [JsonPropertyName("drugieImiona")]
    public string? DrugieImiona { get; set; }

    [JsonPropertyName("nazwisko")]
    public string? Nazwisko { get; set; }

    [JsonPropertyName("plec")]
    public string? Plec { get; set; }
    
    [JsonIgnore]
    public string ImionaINazwisko =>
        string.Join(" ", new string?[] { Imie, DrugieImiona, Nazwisko }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}

// ── Osoba ─────────────────────────────────────────────────────────────────────

public class OsobaPowiazana
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>"osoba" lub "osoba-bez-pesel"</summary>
    [JsonPropertyName("typ")]
    public string Typ { get; set; } = "osoba";

    [JsonPropertyName("tozsamosc")]
    public Tozsamosc? Tozsamosc { get; set; }

    [JsonPropertyName("krsPowiazaniaLiczby")]
    public PowiazaniaLiczby? KrsPowiazaniaLiczby { get; set; }

    [JsonPropertyName("krsPowiazaniaKwerendowane")]
    public List<PowiazanieKwerendowane> KrsPowiazaniaKwerendowane { get; set; } = [];
}

// ── Organizacja ───────────────────────────────────────────────────────────────

public class PowiazaniaNazwy
{
    [JsonPropertyName("pelna")]
    public string? Pelna { get; set; }

    [JsonPropertyName("skrocona")]
    public string? Skrocona { get; set; }
}

public class PowiazaniaNumery
{
    [JsonPropertyName("krs")]
    public string? Krs { get; set; }

    [JsonPropertyName("nip")]
    public string? Nip { get; set; }

    [JsonPropertyName("regon")]
    public string? Regon { get; set; }

    [JsonPropertyName("duns")]
    public string? Duns { get; set; }
}

public class PowiazaniaAdres
{
    [JsonPropertyName("kod")]
    public string? Kod { get; set; }

    [JsonPropertyName("miejscowosc")]
    public string? Miejscowosc { get; set; }

    [JsonPropertyName("nrDomu")]
    public string? NrDomu { get; set; }

    [JsonPropertyName("nrMieszkania")]
    public string? NrMieszkania { get; set; }

    [JsonPropertyName("ulica")]
    public string? Ulica { get; set; }

    [JsonPropertyName("panstwo")]
    public string? Panstwo { get; set; }
}

public class PowiazaniaStan
{
    [JsonPropertyName("formaPrawna")]
    public string? FormaPrawna { get; set; }

    [JsonPropertyName("pkdPrzewazajaceDzial")]
    public string? PkdPrzewazajaceDzial { get; set; }

    [JsonPropertyName("czyWykreslona")]
    public bool CzyWykreslona { get; set; }

    [JsonPropertyName("wLikwidacji")]
    public bool WLikwidacji { get; set; }

    [JsonPropertyName("wUpadlosci")]
    public bool WUpadlosci { get; set; }

    [JsonPropertyName("wZawieszeniu")]
    public bool WZawieszeniu { get; set; }

    [JsonPropertyName("wielkosc")]
    public string? Wielkosc { get; set; }
}

public class PowiazaniaGlownaOsoba
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("imionaINazwisko")]
    public string? ImionaINazwisko { get; set; }
}

public class OrganizacjaPowiazana
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("typ")]
    public string Typ { get; set; } = "organizacja";

    [JsonPropertyName("nazwy")]
    public PowiazaniaNazwy? Nazwy { get; set; }

    [JsonPropertyName("numery")]
    public PowiazaniaNumery? Numery { get; set; }

    [JsonPropertyName("adres")]
    public PowiazaniaAdres? Adres { get; set; }

    [JsonPropertyName("stan")]
    public PowiazaniaStan? Stan { get; set; }

    [JsonPropertyName("glownaOsoba")]
    public PowiazaniaGlownaOsoba? GlownaOsoba { get; set; }

    [JsonPropertyName("krsPowiazaniaLiczby")]
    public PowiazaniaLiczby? KrsPowiazaniaLiczby { get; set; }

    [JsonPropertyName("krsPowiazaniaKwerendowane")]
    public List<PowiazanieKwerendowane> KrsPowiazaniaKwerendowane { get; set; } = [];
}

// ── Struktura główna ──────────────────────────────────────────────────────────

public class PowiazaniaAktualne
{
    [JsonPropertyName("powiazaneOrganizacje")]
    public List<OrganizacjaPowiazana> PowiazaneOrganizacje { get; set; } = [];

    [JsonPropertyName("powiazaneOsobyZProfilem")]
    public List<OsobaPowiazana> PowiazaneOsobyZProfilem { get; set; } = [];

    [JsonPropertyName("powiazaneOsobyBezProfilu")]
    public List<OsobaPowiazana> PowiazaneOsobyBezProfilu { get; set; } = [];

    /// <summary>Wszystkie osoby (z profilem i bez) jako jedna lista.</summary>
    [JsonIgnore]
    public IEnumerable<OsobaPowiazana> WszystkieOsoby =>
        (PowiazaneOsobyZProfilem ?? []).Concat(PowiazaneOsobyBezProfilu ?? []);
}

public class PowiazaniaOrganizacji
{
    [JsonPropertyName("aktualne")]
    public PowiazaniaAktualne? Aktualne { get; set; } = new();
}

/// <summary>
/// Dane firmy będącej centrum grafu (podmiot, którego powiązania opisuje plik).
/// </summary>
public class CentrumFirmy
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nazwy")]
    public PowiazaniaNazwy? Nazwy { get; set; }

    [JsonPropertyName("numery")]
    public PowiazaniaNumery? Numery { get; set; }

    [JsonPropertyName("stan")]
    public PowiazaniaStan? Stan { get; set; }

    [JsonPropertyName("adres")]
    public PowiazaniaAdres? Adres { get; set; }
}

/// <summary>Korzeń pliku dane.json.</summary>
public class DaneRoot
{
    /// <summary>
    /// Opcjonalne metadane firmy-centrum (np. Allegro).
    /// </summary>
    [JsonPropertyName("centrum")]
    public CentrumFirmy? Centrum { get; set; }

    [JsonPropertyName("powiazaniaOrganizacji")]
    public PowiazaniaOrganizacji? PowiazaniaOrganizacji { get; set; } = new();
}

// ─── Graph DTO – wyjście do Vue Flow ─────────────────────────────────────────

public sealed record GraphDto
{
    public List<NodeDto> Nodes { get; init; } = [];
    public List<EdgeDto> Edges { get; set; } = [];
}

public sealed record NodeDto
{
    public string     Id       { get; init; } = string.Empty;
    public string     Type     { get; init; } = "default";
    public PositionDto Position { get; init; } = new();
    public NodeDataDto Data    { get; init; } = new();
}

public sealed record PositionDto
{
    public double X { get; init; }
    public double Y { get; init; }
}

public sealed record NodeDataDto
{
    public string  Label        { get; init; } = string.Empty;
    public string  EntityType   { get; init; } = string.Empty;
    public string? FormaPrawna  { get; init; }
    public string? PkdDzial     { get; init; }
    public string? Krs          { get; init; }
    public string? Nip          { get; init; }
    public string? Wielkosc     { get; init; }
    public bool    IsActive     { get; init; } = true;
    public bool    WLikwidacji  { get; init; }
    public bool    WUpadlosci   { get; init; }
    public bool    BezProfilu   { get; init; }
    public bool    IsCentrum    { get; init; }
    public bool IsSubsidiary { get; set; }
}

public sealed record EdgeDto
{
    public string  Id     { get; init; } = string.Empty;
    public string  Source { get; init; } = string.Empty;
    public string  Target { get; init; } = string.Empty;
    public string? Label  { get; init; }
    public EdgeDataDto Data { get; init; } = new();
}

public sealed record EdgeDataDto
{
    public string  RelationType  { get; init; } = string.Empty;
    public string? RelationLabel { get; init; }
    public string? Kierunek      { get; init; }
    public string? DataStart     { get; init; }
    public string? DataKoniec    { get; init; }
    public bool    IsActive      => DataKoniec is null;
}

// ─── Search / Detail DTOs ─────────────────────────────────────────────────────

public record EntitySummaryDto
{
    public string  Id         { get; init; } = string.Empty;
    public string  Label      { get; init; } = string.Empty;
    public string  EntityType { get; init; } = string.Empty;
    public string? Krs        { get; init; }
    public string? Nip        { get; init; }
}

public record EntityDetailDto : EntitySummaryDto
{
    public string? Nip         { get; init; }
    public string? Regon       { get; init; }
    public string? FormaPrawna { get; init; }
    public string? PkdDzial    { get; init; }
    public string? Adres       { get; init; }
    public bool    WLikwidacji { get; init; }
    public bool    WUpadlosci  { get; init; }
    public List<ConnectionSummaryDto> Connections { get; init; } = [];
}

public sealed record ConnectionSummaryDto
{
    public string  TargetId     { get; init; } = string.Empty;
    public string  TargetLabel  { get; init; } = string.Empty;
    public string  RelationType { get; init; } = string.Empty;
    public string? Opis         { get; init; }
    public string? DataStart    { get; init; }
    public bool    IsActive     { get; init; }
}