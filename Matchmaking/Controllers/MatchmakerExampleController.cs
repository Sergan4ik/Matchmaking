using System.Collections.Concurrent;
using Matchmaking.Matchmaker;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Matchmaking.Controllers;

[ApiController]
[Route("")]
public class MatchmakerExampleController: Controller, IMatchmakerEndpoint
{
    private readonly ILogger<MatchmakerExampleController> _logger;
    private readonly MatchmakerDuelExample matchmaker;

    public MatchmakerExampleController(ILogger<MatchmakerExampleController> logger, MatchmakerDuelExample matchmaker)
    {
        _logger = logger;
        this.matchmaker = matchmaker;
    }

    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok("Hello from MatchmakerExampleController!");
    }

    [HttpPost("StartSearch")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StartSearchResponse), StatusCodes.Status200OK)]
    public IActionResult StartSearchReq([FromBody] SBMMPlayerInfo playerInfo)
    {
        StartSearch(playerInfo);
        _logger.LogInformation($"Player {playerInfo.playerId} started search with rating {playerInfo.rating}.");
        return Ok(new StartSearchResponse("Search started", playerInfo.playerId));
    }

    [HttpPost("CancelSearch")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CancelSearchResponse), StatusCodes.Status200OK)]
    public IActionResult CancelSearchReq(int playerId)
    {
        CancelSearch(playerId);
        _logger.LogInformation($"Player {playerId} cancelled search.");
        return Ok(new CancelSearchResponse("Search cancelled", playerId));
    }

    [HttpPost("AcceptRoom")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AcceptRoomResponse), StatusCodes.Status200OK)]
    public IActionResult AcceptRoomReq(int playerId, int roomId)
    {
        matchmaker.PlayerAcceptedRoom(playerId, roomId);
        _logger.LogInformation($"Player {playerId} accepted room {roomId}.");
        return Ok(new { message = "Room accepted", playerId, roomId });
    }

    [HttpPost("RejectRoom")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RejectRoomResponse), StatusCodes.Status200OK)]
    public IActionResult RejectRoomReq(int playerId, int roomId)
    {
        matchmaker.PlayerRejectedRoom(playerId, roomId);
        _logger.LogInformation($"Player {playerId} rejected room {roomId}.");
        return Ok(new { message = "Room rejected", playerId, roomId });
    }

    [HttpPost("GetMMStatus")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MatchmakingStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetMMStatusReq(int playerId)
    {
        var status = GetMMStatus(playerId);
        return Ok(status);
    }
    
    [HttpPost("FetchRoomState")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MMRoom), StatusCodes.Status200OK)]
    public async Task<IActionResult> FetchRoomStateReq(int roomId)
    {
        var room = await FetchRoomState(roomId);
        if (room == null)
        {
            _logger.LogWarning($"Room {roomId} not found.");
            return NotFound(new { message = "Room not found", roomId });
        }
        return Ok(room);
    }

    [NonAction]
    public Task StartSearch(SBMMPlayerInfo playerInfo)
    {
        matchmaker.PlayerStartedSearch(playerInfo);
        return Task.CompletedTask;
    }

    [NonAction]
    public Task CancelSearch(int playerId)
    {
        matchmaker.PlayerCancelledSearch(playerId);
        return Task.CompletedTask;
    }

    [NonAction]
    public Task AcceptRoom(int playerId, int roomId)
    {
        matchmaker.PlayerAcceptedRoom(playerId, roomId);
        return Task.CompletedTask;
    }

    [NonAction]
    public Task RejectRoom(int playerId, int roomId)
    {
        matchmaker.PlayerRejectedRoom(playerId, roomId);
        return Task.CompletedTask;
    }

    [NonAction]
    public Task<MatchmakingStatusResponse> GetMMStatus(int playerId)
    {
        if (matchmaker.pendingAcceptRooms.TryGetValue(playerId, out var room))
        {
            _logger.LogInformation($"Player {playerId} is in room {room.id} created at {room.createdAt} with players: {string.Join(", ", room.players.Select(p => p.playerId))}");
            return Task.FromResult(new MatchmakingStatusResponse(
                "Player is in a room",
                room
            ));
        }
        else
        {
            _logger.LogInformation($"Player {playerId} is not in a room. Pending players: {matchmaker.playersQueue.Count}, Cancelled players: {matchmaker.cancelledPlayers.Count}");
            return Task.FromResult(new MatchmakingStatusResponse(
                "Player is not in a room",
                new MMRoom
                {
                    id = -1,
                    createdAt = 0,
                    players = new List<SBMMPlayerInfo>(),
                    readyPlayers = new ConcurrentBitset(0, false)
                }
            ));
        }

    }

    [NonAction]
    public Task<MMRoom> FetchRoomState(int roomId)
    {
        if (matchmaker.allRooms.TryGetValue(roomId, out var room))
        {
            _logger.LogInformation($"Fetched room {roomId} with players: {string.Join(", ", room.players.Select(p => p.playerId))}");
            return Task.FromResult(room);
        }
        else
        {
            _logger.LogWarning($"Room {roomId} not found.");
            return Task.FromResult((MMRoom)default);
        }
    }
}

public record StartSearchResponse(string message, int playerId)
{
    public override string ToString()
    {
        return $"{{ message = {message}, playerId = {playerId} }}";
    }
}

public record CancelSearchResponse(string message, int playerId)
{
    public override string ToString()
    {
        return $"{{ message = {message}, playerId = {playerId} }}";
    }
}

public record AcceptRoomResponse(string message, int playerId, int roomId)
{
    public override string ToString()
    {
        return $"{{ message = {message}, playerId = {playerId}, roomId = {roomId} }}";
    }
}
public record RejectRoomResponse(string message, int playerId, int roomId)
{
    public override string ToString()
    {
        return $"{{ message = {message}, playerId = {playerId}, roomId = {roomId} }}";
    }
}