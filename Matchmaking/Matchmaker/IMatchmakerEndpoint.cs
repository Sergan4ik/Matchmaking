using System.Text.RegularExpressions;

namespace Matchmaking.Matchmaker;

public interface IMatchmakerEndpoint
{
    Task StartSearch(SBMMPlayerInfo playerInfo);
    Task CancelSearch(int playerId);
    Task AcceptRoom(int playerId, int roomId);
    Task RejectRoom(int playerId, int roomId);
    Task<MatchmakingStatusResponse> GetMMStatus(int playerId);
    Task<MMRoom> FetchRoomState(int roomId);
}

public record MatchmakingStatusResponse(string message, MMRoom room)
{
    public record PlayerInfo(int playerId, int rating);

    public override string ToString()
    {
        return $"{{ message = {message}, roomId = {room.id}, createdAt = {room.createdAt}, players = [{string.Join(", ", room.players)}], readyPlayers = {room.readyPlayers} }}";
    }
}
