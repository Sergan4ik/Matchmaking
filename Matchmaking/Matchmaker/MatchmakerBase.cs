using System.Collections;
using System.Collections.Concurrent;
using ZergRush.ReactiveCore;

namespace Matchmaking.Matchmaker;

public abstract class MatchmakerBase<T>(ILogger<MatchmakerBase<T>> logger) where T : SBMMPlayerInfo
{
    public ConcurrentQueue<T> playersQueue = new ConcurrentQueue<T>();
    public ConcurrentQueue<int> cancelledPlayers = new ConcurrentQueue<int>();
    public ConcurrentDictionary<int, MMRoom> pendingAcceptRooms = new ConcurrentDictionary<int, MMRoom>();
    public ConcurrentDictionary<int, MMRoom> allRooms = new ConcurrentDictionary<int, MMRoom>();
    
    public EventStream<(MMRoom room , RoomReleaseReason reason)> onRoomReleased = new EventStream<(MMRoom room, RoomReleaseReason reason)>();

    public virtual void PlayerStartedSearch(T playerInfo)
    {
        playersQueue.Enqueue(playerInfo);
        logger.LogInformation($"Player {playerInfo.playerId} started search with rating {playerInfo.rating}. Queue size: {playersQueue.Count + pendingAcceptRooms.Count}");
    }

    public virtual void PlayerCancelledSearch(int playerId)
    {
        cancelledPlayers.Enqueue(playerId);
        logger.LogInformation($"Player {playerId} cancelled search. MM queue size: {playersQueue.Count + pendingAcceptRooms.Count}, Cancelled players: {cancelledPlayers.Count}");
    }
    public abstract void PlayerAcceptedRoom(int playerId, int roomId);
    public abstract void PlayerRejectedRoom(int playerId, int roomId);
    
    protected int __roomIdFactory = 0;
    protected virtual MMRoom CreateRoom(IEnumerable<T> players)
    {
        var playerInfos = players.ToList<SBMMPlayerInfo>();
        var mmRoom = new MMRoom()
        {
            id = __roomIdFactory++,
            createdAt = (int)GlobalTime.seconds,
            players = playerInfos,
            readyPlayers = new ConcurrentBitset(playerInfos.Count(), false)
        };
        allRooms.TryAdd(mmRoom.id, mmRoom);
        
        logger.LogInformation($"Room {mmRoom.id} created, containing players: {string.Join(", ", mmRoom.players.Select(p => p.playerId))}");
        return mmRoom;
    }

    protected virtual void ReleaseRoomFromMM(int roomId, RoomReleaseReason reason)
    {
        if (allRooms.TryGetValue(roomId, out var room))
        {
            foreach (var p in room.players)
            {
                if (pendingAcceptRooms.TryRemove(p.playerId, out _)) { }
            }
            
            onRoomReleased.Send((room, reason));
            logger.LogInformation($"Room {roomId} released due to {reason}. Remaining rooms: {allRooms.Count}");
        }
    }
}

public enum RoomReleaseReason
{
    RoomAccepted,
    RoomTimeout,
    RoomCancelled
}