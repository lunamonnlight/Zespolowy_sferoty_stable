using Sferity.Backend.Models;

namespace Sferity.Backend.Services;

public interface IConnectionsService
{
    /// <summary>
    /// Zwraca pełny graf centrum (wszystkie znane powiązania z pliku).
    /// entityId == null → pełny graf Allegro.
    /// entityId podane  → ego-graf tej konkretnej encji.
    /// </summary>
    Task<GraphDto> GetGraphAsync(string? entityId = null);

    /// <summary>
    /// Zwraca TYLKO powiązania węzła których jeszcze nie ma w grafie.
    /// knownNodeIds – id węzłów które frontend już wyświetla.
    /// Gdy brak danych dla encji zwraca pusty graf (nie rzuca wyjątku).
    /// </summary>
    Task<GraphDto> ExpandNodeAsync(string nodeId, HashSet<string> knownNodeIds);

    /// <summary>
    /// Przeszukuje załadowane encje po nazwie / NIP / KRS.
    /// </summary>
    Task<IEnumerable<EntitySummaryDto>> SearchEntitiesAsync(string query);

    /// <summary>
    /// Zwraca pełne dane encji wraz z listą powiązań.
    /// </summary>
    Task<EntityDetailDto?> GetEntityDetailAsync(string id);
}