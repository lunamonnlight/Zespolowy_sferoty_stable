using Microsoft.AspNetCore.Mvc;
using Sferity.Backend.Models;
using Sferity.Backend.Services;

namespace Sferity.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessConnectionsController : ControllerBase
{
    private readonly IConnectionsService _connectionsService;
    private readonly ILogger<BusinessConnectionsController> _logger;

    public BusinessConnectionsController(
        IConnectionsService connectionsService,
        ILogger<BusinessConnectionsController> logger)
    {
        _connectionsService = connectionsService;
        _logger = logger;
    }
    
    [HttpGet("graph")]
    public async Task<ActionResult<GraphDto>> GetGraph([FromQuery] string? entityId = null)
    {
        try
        {
            var graph = await _connectionsService.GetGraphAsync(entityId);
            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania grafu dla entityId={EntityId}", entityId);
            return StatusCode(500, new { error = "Błąd serwera przy pobieraniu grafu." });
        }
    }

    /// <summary>
    /// Rozwinięcie węzła – zwraca tylko nowe węzły i krawędzie (te których nie ma w knownNodeIds).
    /// </summary>
    [HttpPost("expand/{nodeId}")]
    public async Task<ActionResult<GraphDto>> ExpandNode(
        string nodeId,
        [FromBody] HashSet<string> knownNodeIds)
    {
        try
        {
            var graph = await _connectionsService.ExpandNodeAsync(nodeId, knownNodeIds ?? []);
            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas rozwijania węzła nodeId={NodeId}", nodeId);
            return StatusCode(500, new { error = "Błąd serwera przy rozwijaniu węzła." });
        }
    }

    /// <summary>
    /// Wyszukiwanie encji po nazwie, KRS lub NIP.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EntitySummaryDto>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Parametr 'query' jest wymagany." });

        var results = await _connectionsService.SearchEntitiesAsync(query);
        return Ok(results);
    }

    /// <summary>
    /// Szczegóły konkretnej encji.
    /// </summary>
    [HttpGet("entity/{id}")]
    public async Task<ActionResult<EntityDetailDto>> GetEntity(string id)
    {
        var entity = await _connectionsService.GetEntityDetailAsync(id);
        if (entity is null)
            return NotFound(new { error = $"Encja o id={id} nie istnieje." });

        return Ok(entity);
    }
}