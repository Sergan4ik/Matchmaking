using System.Collections;

namespace Matchmaking.Matchmaker;

public class MatchmakerDuelExample(
    ILogger<MatchmakerDuelExample> logger,
    int startRatingDelta = 10,
    int stepSize = 20,
    int maxSteps = 10,
    int increaseStepTime = 20,
    int acceptTimeout = 30)
    : MatchmakerBase<SBMMPlayerInfo>(logger)
{
    private Task _solveTask = Task.CompletedTask;
    private int _idleTimeout => increaseStepTime * (maxSteps + 2); // +2 to ensure we have time to process the last steps
    
    // private SortedList<int, (SBMMPlayerInfo info, long ts)> _players = new SortedList<int, (SBMMPlayerInfo info, long ts)>();
    private Dictionary<int, (SBMMPlayerInfo player, long ts)> _players = new Dictionary<int, (SBMMPlayerInfo player, long ts)>();
    

    public override void PlayerStartedSearch(SBMMPlayerInfo info)
    {
        base.PlayerStartedSearch(info);
        if (_solveTask.IsCompleted)
        {
            _solveTask = Task.Run(SolveRooms);
        }
    }

    private static ConcurrentBitset readyRoom = new ConcurrentBitset(2, true);
    public override void PlayerAcceptedRoom(int playerId, int roomId)
    {
        var roomExists = pendingAcceptRooms.TryGetValue(playerId, out var mmRoom);
        if (roomExists == false || mmRoom.id != roomId)
        {
            logger.LogWarning($"Player {playerId} tried to accept room {roomId}, but it was not found in pending rooms.");
            return;
        }
        
        logger.LogInformation($"Player {playerId} accepted room {roomId}.");
        mmRoom.readyPlayers[mmRoom.players.FindIndex(p => p.playerId == playerId)] = true;

        if (mmRoom.readyPlayers.IsSame(readyRoom)) 
        {
            logger.LogInformation($"All players in room {roomId} are ready. Starting the game.");
            ReleaseRoomFromMM(mmRoom.id, RoomReleaseReason.RoomAccepted);
        }
        else
        {
            logger.LogInformation($"Player {playerId} accepted room {roomId}, but not all players are ready yet.");
        }
    }

    public override void PlayerRejectedRoom(int playerId, int roomId)
    {
        var roomExists = pendingAcceptRooms.TryGetValue(playerId, out var mmRoom);
        if (roomExists == false || mmRoom.id != roomId)
        {
            logger.LogWarning($"Player {playerId} tried to reject room {roomId}, but it was not found in pending rooms.");
            return;
        }
        
        logger.LogInformation($"Player {playerId} rejected room {roomId}.");
        ReleaseRoomFromMM(roomId, RoomReleaseReason.RoomCancelled);
    }

    protected async Task SolveRooms()
    {
        try
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(_idleTimeout * 1000);
            
            while (!cts.IsCancellationRequested)
            {
                ProcessRoomAcceptanceTimeout();

                while (playersQueue.TryDequeue(out var playerInfo))
                    _players.Add(playerInfo.playerId,(playerInfo, GlobalTime.seconds));
                
                var newRooms = await CalculateRoomsInTheMoment();
                if (newRooms.Count > 0)
                    cts.CancelAfter(_idleTimeout * 1000);
                
                ProcessNewRooms(newRooms);
                ProcessCancelledPlayers();
                
                await Task.Delay(1000, cts.Token); 
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
            logger.LogDebug("Matchmaking process was cancelled due to idle timeout.");
        }
        catch (Exception e)
        {
            logger.LogError($"Error in MatchmakerExample: {e.Message}");
        }
    }

    private void ProcessCancelledPlayers()
    {
        while (cancelledPlayers.TryDequeue(out var cancelledPlayer))
        {
            _players.Remove(cancelledPlayer);
            //process room
            if (pendingAcceptRooms.TryGetValue(cancelledPlayer, out var mmRoom))
            {
                ReleaseRoomFromMM(mmRoom.id, RoomReleaseReason.RoomCancelled);
            }
        }
    }

    private void ProcessNewRooms(List<MMRoom> newRooms)
    {
        foreach (var room in newRooms)
        {
            for (var i = room.players.Count - 1; i >= 0; i--)
            {
                var player = room.players[i];
                        
                if (pendingAcceptRooms.AddOrUpdate(player.playerId, p => room, (p, curRoom) => room) == room)
                {
                    _players.Remove(player.playerId);
                    logger.LogInformation(
                        $"Room {room.id} created for player {player.playerId} with rating {player.rating}.");
                }
                else
                {
                    logger.LogWarning($"Failed to add player {player.playerId} to room {room.id}. Room already exists or update failed.");
                }
            }
        }
    }

    private void ProcessRoomAcceptanceTimeout()
    {
        List<int> roomsToRemove = new List<int>();
        foreach (var (id, mmRoom) in allRooms)
        {
            if (GlobalTime.seconds - mmRoom.createdAt > acceptTimeout)
            {
                roomsToRemove.Add(id);
            }
        }
        foreach (var id in roomsToRemove)
        {
            ReleaseRoomFromMM(id, RoomReleaseReason.RoomTimeout);
        }
    }

    private async Task<List<MMRoom>> CalculateRoomsInTheMoment()
    {
        List<MMRoom> rooms = new List<MMRoom>();
        List<(int, List<int>)> deltaGroups = new List<(int, List<int>)>(_players.Count);
        List<(SBMMPlayerInfo info, long ts)> sortedPlayers = _players.Values
            .Select(p => p)
            .OrderBy(p => p.player.rating)
            .ToList();

        for (var i = 0; i < sortedPlayers.Count; i++)
        {
            var p1 = sortedPlayers[i];
            long steps = Math.Min((GlobalTime.seconds - sortedPlayers[i].ts) / increaseStepTime, maxSteps);
            int playerDelta = (int)(startRatingDelta + stepSize * steps);
            int ptrL = sortedPlayers.LowerBound(p1.info.rating - playerDelta, (t, val) => t.info.rating.CompareTo(val));
            int ptrR = sortedPlayers.LowerBound(p1.info.rating + playerDelta, (t, val) => t.info.rating.CompareTo(val));

            List<int> group = new List<int>();
            if (ptrL < ptrR)
            {
                for (int j = ptrL; j < ptrR; j++)
                {
                    if (j != i)
                    {
                        group.Add(j);
                    }
                }

            }
            deltaGroups.Add((i, group));
        }

        deltaGroups.Sort((a, b) => a.Item2.Count.CompareTo(b.Item2.Count));

        HashSet<int> usedPlayers = new HashSet<int>();
        foreach (var (playerInternalId, neighbours) in deltaGroups)
        {
            if (neighbours.Count == 0) continue;
            if (usedPlayers.Contains(playerInternalId)) continue;

            int playerId1 = sortedPlayers[playerInternalId].info.playerId;
            int playerId2 = sortedPlayers[neighbours[0]].info.playerId;
            
            var p1 = _players[playerId1].player;
            var p2 = _players[playerId2].player;

            usedPlayers.Add(playerInternalId);
            usedPlayers.Add(neighbours[0]);
            rooms.Add(CreateRoom([p1, p2]));
        }

        return rooms;
    }
}