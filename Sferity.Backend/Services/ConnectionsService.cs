using System.Text.Json;
using Sferity.Backend.Models;

namespace Sferity.Backend.Services;

/// <summary>
/// Buduje graf powiązań z perspektywy jednej firmy-centrum (np. Allegro).
///
/// Semantyka pliku dane.json:
///   centrum                        – firma której powiązania oglądamy (węzeł centralny)
///   powiazaneOrganizacje[]         – spółki córki / powiązane org (Allegro ma w nich udziały)
///   powiazaneOsobyZProfilem[]      – osoby w zarządzie / prokurze centrum
///   powiazaneOsobyBezProfilu[]     – j.w. bez numeru PESEL (zagraniczni)
///
/// Kierunki krawędzi w grafie:
///   osoba     → centrum   (KRS_BOARD, KRS_PROXY)    – osoba pełni funkcję w centrum
///   centrum   → org-córka (KRS_SHAREHOLDER)         – centrum ma udziały w spółce
/// </summary>
public class ConnectionsService : IConnectionsService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConnectionsService> _logger;

    private GraphData? _cachedData;
    private readonly object LoadLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // Id centrum w grafie – stała by krawędzie mogły się do niego odwoływać
    private const string CentrumNodeId = "org-centrum";

    public ConnectionsService(
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ConnectionsService> logger)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public async Task<GraphDto> GetGraphAsync(string? entityId = null)
    {
        var data = await LoadDataAsync();
        return entityId is null
            ? BuildFullGraph(data)
            : BuildEntityGraph(data, entityId);
    }

    public async Task<IEnumerable<EntitySummaryDto>> SearchEntitiesAsync(string query)
    {
        var data = await LoadDataAsync();
        var q = query.ToLowerInvariant();

        // Centrum zawsze na początku wyników jeśli pasuje
        var centrumMatch = data.Centrum.Label.ToLowerInvariant().Contains(q)
            ? new EntitySummaryDto
              {
                  Id         = CentrumNodeId,
                  Label      = data.Centrum.Label,
                  EntityType = "organizacja",
                  Krs        = data.Centrum.Krs
              }
            : null;

        var personResults = data.Persons
            .Where(p => p.Tozsamosc?.ImionaINazwisko.ToLowerInvariant().Contains(q) == true)
            .Select(p => new EntitySummaryDto
            {
                Id         = $"person-{p.Id}",
                Label      = p.Tozsamosc!.ImionaINazwisko,
                EntityType = p.Typ
            });

        var orgResults = data.Subsidiaries
            .Where(o =>
                o.Nazwy?.Pelna?.ToLowerInvariant().Contains(q) == true ||
                o.Nazwy?.Skrocona?.ToLowerInvariant().Contains(q) == true ||
                o.Numery?.Krs?.Contains(q) == true ||
                o.Numery?.Nip?.Contains(q) == true)
            .Select(o => new EntitySummaryDto
            {
                Id         = $"org-{o.Id}",
                Label      = o.Nazwy?.Skrocona ?? o.Nazwy?.Pelna ?? o.Id.ToString(),
                EntityType = "organizacja",
                Krs        = o.Numery?.Krs
            });

        var all = personResults.Concat(orgResults);
        if (centrumMatch is not null)
            all = all.Prepend(centrumMatch);

        return all.Take(50);
    }

    public async Task<EntityDetailDto?> GetEntityDetailAsync(string id)
    {
        var data = await LoadDataAsync();

        if (id == CentrumNodeId)
            return BuildCentrumDetail(data);

        var personId = id.StartsWith("person-") ? id["person-".Length..] : id;
        var person = data.Persons.FirstOrDefault(p => p.Id.ToString() == personId);
        if (person is not null)
            return BuildPersonDetail(person, data);

        var orgId = id.StartsWith("org-") ? id["org-".Length..] : id;
        var org = data.Subsidiaries.FirstOrDefault(o => o.Id.ToString() == orgId);
        if (org is not null)
            return BuildOrgDetail(org, data);

        return null;
    }

    /// <summary>
    /// Zwraca TYLKO nowe węzły i krawędzie dla danego węzła.
    /// knownNodeIds – id węzłów już obecnych w grafie frontendowym.
    ///
    /// Logika:
    ///   org-centrum   → brak sensu rozwijać (pełny graf już załadowany przy wyszukaniu)
    ///   person-{id}   → pobieramy ego-graf tej osoby z lokalnych danych,
    ///                   filtrujemy węzły których frontend jeszcze nie ma
    ///   org-{id}      → j.w. dla organizacji-córki
    ///   Gdy brak danych → zwraca pusty GraphDto (nigdy nie rzuca wyjątku)
    /// </summary>
    public async Task<GraphDto> ExpandNodeAsync(string nodeId, HashSet<string> knownNodeIds)
    {
        // Centrum nie wymaga rozwinięcia – pełny graf jest już załadowany
        if (nodeId == CentrumNodeId)
            return new GraphDto();

        var data = await LoadDataAsync();

        // Pobierz ego-graf tego węzła z lokalnych danych
        var egoGraph = BuildEntityGraph(data, nodeId);

        // Gdy brak danych (np. kliknięto spółkę i nie mamy jej wewnętrznych powiązań)
        // BuildEntityGraph zwróci graf tylko z tym węzłem i centrum – filtrujemy znane
        var newNodes = egoGraph.Nodes
            .Where(n => !knownNodeIds.Contains(n.Id))
            .ToList();

        var newEdges = egoGraph.Edges
            .Where(e => !knownNodeIds.Contains(e.Id))   // edge.Id nie koliduje z node.Id
            .ToList();

        // Dla krawędzi: source/target muszą albo już być w grafie albo być w newNodes
        var allAvailableIds = knownNodeIds
            .Concat(newNodes.Select(n => n.Id))
            .ToHashSet();

        newEdges = newEdges
            .Where(e => allAvailableIds.Contains(e.Source) && allAvailableIds.Contains(e.Target))
            .ToList();

        return new GraphDto { Nodes = newNodes, Edges = newEdges };
    }

    // ─── Data loading ────────────────────────────────────────────────────────

    private async Task<GraphData> LoadDataAsync()
    {
        if (_cachedData is not null) return _cachedData;

        string? pathToLoad;
        lock (LoadLock)
        {
            if (_cachedData is not null) return _cachedData;

            pathToLoad = Path.Combine(_env.ContentRootPath, "Files", "powiazania2.json");

            if (!File.Exists(pathToLoad))
            {
                _logger.LogWarning("Plik dane.json nie istnieje ({Path}). Zwracam pusty graf.", pathToLoad);
                _cachedData = BuildEmptyGraphData();
                return _cachedData;
            }
        }

        var json = await File.ReadAllTextAsync(pathToLoad);

        lock (LoadLock)
        {
            if (_cachedData is not null) return _cachedData;

            var root = JsonSerializer.Deserialize<DaneRoot>(json, JsonOptions);
            _cachedData = ExtractGraphData(root, centrumKrsHint: null);

            _logger.LogInformation(
                "Załadowano dane centrum={Centrum}, {Persons} osób, {Orgs} spółek powiązanych.",
                _cachedData.Centrum.Label,
                _cachedData.Persons.Count,
                _cachedData.Subsidiaries.Count);

            return _cachedData;
        }
    }

    /// <summary>
    /// Wyciąga dane z DaneRoot i buduje GraphData z centrum jako wyraźnym węzłem.
    ///
    /// Centrum jest ustalane przez (w kolejności priorytetu):
    ///   1. Pole "centrum" w JSON (gdy dostarczymy własne dane firmy w pliku)
    ///   2. centrumKrsHint – KRS przekazany przy proxy do rejestr.io
    ///   3. Konfiguracja appsettings: Centrum:Nazwa, Centrum:Krs
    ///   4. Fallback: "Firma" bez numerów
    /// </summary>
    private GraphData ExtractGraphData(DaneRoot? root, string? centrumKrsHint)
    {
        if (root is null) return BuildEmptyGraphData();

        // Null-safe dostęp na każdym poziomie zagnieżdżenia.
        // System.Text.Json nie gwarantuje że właściwości z "= new()" jako field
        // initializer zostaną zachowane po deserializacji – ustawia je przez
        // reflection nadpisując inicjalizatory. Używamy ?. i ?? new() jako ochrony.
        var aktualne = root.PowiazaniaOrganizacji?.Aktualne ?? new PowiazaniaAktualne();

        // Ustal centrum
        var centrum = BuildCentrumInfo(root.Centrum, centrumKrsHint);

        // Osoby: zarząd + prokurenci centrum (wszystkie sekcje osób).
        // Każda lista jest null-safe przez właściwość WszystkieOsoby.
        var persons = aktualne.WszystkieOsoby.ToList();

        // Organizacje powiązane: spółki córki (centrum ma w nich udziały)
        var subsidiaries = aktualne.PowiazaneOrganizacje ?? [];

        return new GraphData
        {
            Centrum      = centrum,
            Persons      = persons,
            Subsidiaries = subsidiaries
        };
    }

    /// <summary>
    /// Buduje metadane centrum z dostępnych źródeł.
    /// </summary>
    private CentrumInfo BuildCentrumInfo(CentrumFirmy? fromJson, string? krsHint)
    {
        // 1. Z JSON
        if (fromJson is not null)
            return new CentrumInfo
            {
                Label       = fromJson.Nazwy?.Skrocona ?? fromJson.Nazwy?.Pelna ?? "Centrum",
                Krs         = fromJson.Numery?.Krs,
                Nip         = fromJson.Numery?.Nip,
                FormaPrawna = fromJson.Stan?.FormaPrawna,
                IsActive    = fromJson.Stan?.CzyWykreslona == false
            };

        // 2. Z KRS hinta (proxy rejestr.io)
        if (krsHint is not null)
            return new CentrumInfo { Label = $"KRS {krsHint}", Krs = krsHint };

        // 3. Z appsettings
        var nazwa = "Allegro";
        var krs   = "0000808664";
        if (nazwa is not null)
            return new CentrumInfo { Label = nazwa, Krs = krs };

        // 4. Fallback
        return new CentrumInfo { Label = "Firma" };
    }

    private GraphData BuildEmptyGraphData() => new()
    {
        // Centrum z konfiguracji nawet gdy plik jest pusty/brakuje
        Centrum      = BuildCentrumInfo(fromJson: null, krsHint: null),
        Persons      = [],
        Subsidiaries = []
    };

    // ─── Graph building ──────────────────────────────────────────────────────

    /// <summary>
    /// Pełny graf z centrum pośrodku:
    ///
    ///   [Prezes] ──KRS_BOARD──►  [ALLEGRO]  ──KRS_SHAREHOLDER──► [Spółka córka]
    ///   [Prokurent] ──KRS_PROXY──►  [ALLEGRO]
    ///
    /// Centrum na pozycji (0,0). Osoby na wewnętrznym okręgu (r=320).
    /// Spółki córki na zewnętrznym okręgu (r=650).
    /// </summary>
    private static GraphDto BuildFullGraph(GraphData data)
    {
        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();

        // ── Węzeł centrum ──────────────────────────────────────────────────
        nodes.Add(new NodeDto
        {
            Id       = CentrumNodeId,
            Type     = "organization-centrum",
            Position = new PositionDto { X = 0, Y = 0 },
            Data     = new NodeDataDto
            {
                Label       = data.Centrum.Label,
                EntityType  = "organizacja",
                Krs         = data.Centrum.Krs,
                Nip         = data.Centrum.Nip,
                FormaPrawna = data.Centrum.FormaPrawna,
                IsActive    = data.Centrum.IsActive,
                IsCentrum   = true
            }
        });

        // ── Węzły osób + krawędzie osoba → centrum ────────────────────────
        foreach (var (person, i) in data.Persons.Select((p, i) => (p, i)))
        {
            var personNodeId = $"person-{person.Id}";

            nodes.Add(new NodeDto
            {
                Id       = personNodeId,
                Type     = person.Typ == "osoba-bez-pesel" ? "person-foreign" : "person",
                Position = CirclePosition(i, data.Persons.Count, radius: 320),
                Data     = BuildPersonNodeData(person)
            });

            // Każda rola osoby = osobna krawędź do centrum
            foreach (var rel in person.KrsPowiazaniaKwerendowane)
                edges.Add(BuildEdge(
                    source:   personNodeId,
                    target:   CentrumNodeId,
                    sourceId: person.Id.ToString(),
                    targetId: "centrum",
                    rel:      rel));
        }

        // ── Węzły spółek córek + krawędzie centrum → spółka ───────────────
        foreach (var (sub, i) in data.Subsidiaries.Select((o, i) => (o, i)))
        {
            var subNodeId = $"org-{sub.Id}";

            nodes.Add(new NodeDto
            {
                Id       = subNodeId,
                Type     = "organization",
                Position = CirclePosition(i, data.Subsidiaries.Count, radius: 650),
                Data     = BuildOrgNodeData(sub, isSubsidiary: true)
            });

            // Centrum → spółka (centrum jest udziałowcem)
            foreach (var rel in sub.KrsPowiazaniaKwerendowane)
                edges.Add(BuildEdge(
                    source:   CentrumNodeId,
                    target:   subNodeId,
                    sourceId: "centrum",
                    targetId: sub.Id.ToString(),
                    rel:      rel));
        }

        var graph = new GraphDto { Nodes = nodes, Edges = edges };
        DeduplicateEdges(graph);
        return graph;
    }

    /// <summary>
    /// Ego-graf:
    ///   - centrum jako ego → pokazuje wszystkich swoich zarządców i wszystkie spółki córki
    ///   - osoba jako ego  → pokazuje centrum + jej inne org jeśli byłyby w danych
    ///   - spółka córka    → pokazuje tylko centrum jako wspólnika
    /// </summary>
    private static GraphDto BuildEntityGraph(GraphData data, string entityId)
    {
        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();

        // ── Ego = centrum ──────────────────────────────────────────────────
        if (entityId == CentrumNodeId)
            return BuildFullGraph(data); // Centrum = pełny graf

        // ── Ego = osoba ────────────────────────────────────────────────────
        var rawPersonId = entityId.StartsWith("person-") ? entityId["person-".Length..] : null;
        var person = rawPersonId is not null
            ? data.Persons.FirstOrDefault(p => p.Id.ToString() == rawPersonId)
            : null;

        if (person is not null)
        {
            // Węzeł osoby w centrum ego-grafu
            nodes.Add(new NodeDto
            {
                Id       = $"person-{person.Id}",
                Type     = person.Typ == "osoba-bez-pesel" ? "person-foreign" : "person",
                Position = new PositionDto { X = 0, Y = 0 },
                Data     = BuildPersonNodeData(person)
            });

            // Centrum jako jedyny sąsiad (w tym formacie osoba jest tylko w jednej firmie)
            nodes.Add(new NodeDto
            {
                Id       = CentrumNodeId,
                Type     = "organization-centrum",
                Position = CirclePosition(0, 1, radius: 320),
                Data     = new NodeDataDto
                {
                    Label     = data.Centrum.Label,
                    EntityType = "organizacja",
                    Krs       = data.Centrum.Krs,
                    IsCentrum = true,
                    IsActive  = data.Centrum.IsActive
                }
            });

            foreach (var rel in person.KrsPowiazaniaKwerendowane)
                edges.Add(BuildEdge(
                    $"person-{person.Id}", CentrumNodeId,
                    person.Id.ToString(), "centrum", rel));

            var graph = new GraphDto { Nodes = nodes, Edges = edges };
            DeduplicateEdges(graph);
            return graph;
        }

        // ── Ego = spółka córka ─────────────────────────────────────────────
        var rawOrgId = entityId.StartsWith("org-") ? entityId["org-".Length..] : entityId;
        var sub = data.Subsidiaries.FirstOrDefault(o => o.Id.ToString() == rawOrgId);

        if (sub is not null)
        {
            // Spółka córka w centrum ego-grafu
            nodes.Add(new NodeDto
            {
                Id       = $"org-{sub.Id}",
                Type     = "organization",
                Position = new PositionDto { X = 0, Y = 0 },
                Data     = BuildOrgNodeData(sub, isSubsidiary: true)
            });

            // Centrum jako wspólnik
            nodes.Add(new NodeDto
            {
                Id       = CentrumNodeId,
                Type     = "organization-centrum",
                Position = CirclePosition(0, 1, radius: 400),
                Data     = new NodeDataDto
                {
                    Label      = data.Centrum.Label,
                    EntityType = "organizacja",
                    Krs        = data.Centrum.Krs,
                    IsCentrum  = true,
                    IsActive   = data.Centrum.IsActive
                }
            });

            foreach (var rel in sub.KrsPowiazaniaKwerendowane)
                edges.Add(BuildEdge(
                    CentrumNodeId, $"org-{sub.Id}",
                    "centrum", sub.Id.ToString(), rel));
        }

        var g = new GraphDto { Nodes = nodes, Edges = edges };
        DeduplicateEdges(g);
        return g;
    }

    // ─── Node / Edge helpers ─────────────────────────────────────────────────

    private static NodeDataDto BuildPersonNodeData(OsobaPowiazana p) => new()
    {
        Label      = p.Tozsamosc?.ImionaINazwisko ?? p.Id.ToString(),
        EntityType = p.Typ,
        IsActive   = true,
        BezProfilu = p.Typ == "osoba-bez-pesel"
    };

    private static NodeDataDto BuildOrgNodeData(OrganizacjaPowiazana o, bool isSubsidiary) => new()
    {
        Label        = o.Nazwy?.Skrocona ?? o.Nazwy?.Pelna ?? o.Id.ToString(),
        EntityType   = "organizacja",
        FormaPrawna  = o.Stan?.FormaPrawna,
        PkdDzial     = o.Stan?.PkdPrzewazajaceDzial,
        Krs          = o.Numery?.Krs,
        Nip          = o.Numery?.Nip,
        Wielkosc     = o.Stan?.Wielkosc,
        IsActive     = o.Stan?.CzyWykreslona == false,
        WLikwidacji  = o.Stan?.WLikwidacji ?? false,
        WUpadlosci   = o.Stan?.WUpadlosci ?? false,
        IsSubsidiary = isSubsidiary
    };

    private static EdgeDto BuildEdge(
        string source, string target,
        string sourceId, string targetId,
        PowiazanieKwerendowane rel)
    {
        var relLabel = rel.Typ switch
        {
            "KRS_BOARD"       => string.IsNullOrWhiteSpace(rel.Opis) ? "Zarząd" : rel.Opis,
            "KRS_SHAREHOLDER" => "Wspólnik",
            "KRS_FOUNDER"     => "Założyciel",
            "KRS_PROXY"       => "Prokurent",
            _                 => rel.Typ ?? "Powiązanie"
        };

        return new EdgeDto
        {
            Id     = $"edge-{sourceId}-{targetId}-{rel.Typ}",
            Source = source,
            Target = target,
            Label  = relLabel,
            Data   = new EdgeDataDto
            {
                RelationType  = rel.Typ ?? string.Empty,
                RelationLabel = relLabel,
                Kierunek      = rel.Kierunek,
                DataStart     = rel.DataStart,
                DataKoniec    = rel.DataKoniec
            }
        };
    }

    private static void DeduplicateEdges(GraphDto graph)
    {
        graph.Edges = [.. graph.Edges.GroupBy(e => e.Id).Select(g => g.First())];
    }

    private static PositionDto CirclePosition(int index, int total, double radius, double offsetY = 0)
    {
        if (total == 0) return new PositionDto();
        var angle = 2 * Math.PI * index / total;
        return new PositionDto
        {
            X = radius * Math.Cos(angle),
            Y = radius * Math.Sin(angle) + offsetY
        };
    }

    // ─── Detail helpers ──────────────────────────────────────────────────────

    private static EntityDetailDto BuildCentrumDetail(GraphData data)
    {
        // Centrum: powiązania to wszyscy zarządcy + wszystkie spółki córki
        var boardConnections = data.Persons
            .SelectMany(p => p.KrsPowiazaniaKwerendowane.Select(rel => new ConnectionSummaryDto
            {
                TargetId     = $"person-{p.Id}",
                TargetLabel  = p.Tozsamosc?.ImionaINazwisko ?? p.Id.ToString(),
                RelationType = rel.Typ ?? string.Empty,
                Opis         = string.IsNullOrWhiteSpace(rel.Opis) ? null : rel.Opis,
                DataStart    = rel.DataStart,
                IsActive     = rel.DataKoniec is null
            }));

        var subsidiaryConnections = data.Subsidiaries
            .SelectMany(o => o.KrsPowiazaniaKwerendowane.Select(rel => new ConnectionSummaryDto
            {
                TargetId     = $"org-{o.Id}",
                TargetLabel  = o.Nazwy?.Skrocona ?? o.Nazwy?.Pelna ?? o.Id.ToString(),
                RelationType = rel.Typ ?? string.Empty,
                DataStart    = rel.DataStart,
                IsActive     = rel.DataKoniec is null
            }));

        return new EntityDetailDto
        {
            Id          = CentrumNodeId,
            Label       = data.Centrum.Label,
            EntityType  = "organizacja",
            Krs         = data.Centrum.Krs,
            Nip         = data.Centrum.Nip,
            FormaPrawna = data.Centrum.FormaPrawna,
            Connections = [.. boardConnections.Concat(subsidiaryConnections)]
        };
    }

    private static EntityDetailDto BuildPersonDetail(OsobaPowiazana person, GraphData data)
    {
        var connections = person.KrsPowiazaniaKwerendowane
            .Select(rel => new ConnectionSummaryDto
            {
                TargetId     = CentrumNodeId,
                TargetLabel  = data.Centrum.Label,
                RelationType = rel.Typ ?? string.Empty,
                Opis         = string.IsNullOrWhiteSpace(rel.Opis) ? null : rel.Opis,
                DataStart    = rel.DataStart,
                IsActive     = rel.DataKoniec is null
            })
            .ToList();

        return new EntityDetailDto
        {
            Id          = $"person-{person.Id}",
            Label       = person.Tozsamosc?.ImionaINazwisko ?? person.Id.ToString(),
            EntityType  = person.Typ,
            Connections = connections
        };
    }

    private static EntityDetailDto BuildOrgDetail(OrganizacjaPowiazana org, GraphData data)
    {
        // Spółka córka – jedyne znane powiązanie to centrum jako wspólnik
        var connections = org.KrsPowiazaniaKwerendowane
            .Select(rel => new ConnectionSummaryDto
            {
                TargetId     = CentrumNodeId,
                TargetLabel  = data.Centrum.Label,
                RelationType = rel.Typ ?? string.Empty,
                DataStart    = rel.DataStart,
                IsActive     = rel.DataKoniec is null
            })
            .ToList();

        var adresStr = org.Adres is null ? null
            : string.Join(", ",
                new string?[] { org.Adres.Ulica, org.Adres.NrDomu, org.Adres.Kod, org.Adres.Miejscowosc }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

        return new EntityDetailDto
        {
            Id          = $"org-{org.Id}",
            Label       = org.Nazwy?.Skrocona ?? org.Nazwy?.Pelna ?? org.Id.ToString(),
            EntityType  = "organizacja",
            Krs         = org.Numery?.Krs,
            Nip         = org.Numery?.Nip,
            Regon       = org.Numery?.Regon,
            FormaPrawna = org.Stan?.FormaPrawna,
            PkdDzial    = org.Stan?.PkdPrzewazajaceDzial,
            Adres       = adresStr,
            WLikwidacji = org.Stan?.WLikwidacji ?? false,
            WUpadlosci  = org.Stan?.WUpadlosci ?? false,
            Connections = connections
        };
    }

    // ─── Internal types ──────────────────────────────────────────────────────

    /// <summary>Metadane firmy-centrum wyciągnięte z różnych źródeł.</summary>
    private sealed class CentrumInfo
    {
        public string  Label       { get; init; } = "Firma";
        public string? Krs         { get; init; }
        public string? Nip         { get; init; }
        public string? FormaPrawna { get; init; }
        public bool    IsActive    { get; init; } = true;
    }

    private sealed class GraphData
    {
        public CentrumInfo                Centrum      { get; init; } = new();
        public List<OsobaPowiazana>       Persons      { get; init; } = [];
        public List<OrganizacjaPowiazana> Subsidiaries { get; init; } = [];
    }
}