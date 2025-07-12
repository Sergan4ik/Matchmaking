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


