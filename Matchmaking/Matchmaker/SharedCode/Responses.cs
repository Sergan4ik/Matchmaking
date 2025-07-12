#region Basic responses

using System;

[Serializable]
public class StartSearchResponse : BaseData
{
    public string message;
    public int    playerId;

    public StartSearchResponse(string message, int playerId)
    {
        this.message  = message;
        this.playerId = playerId;
    }

    public override string ToString() =>
        $"{{ message = {message}, playerId = {playerId} }}";
}

[Serializable]
public class CancelSearchResponse: BaseData
{
    public string message;
    public int    playerId;

    public CancelSearchResponse(string message, int playerId)
    {
        this.message  = message;
        this.playerId = playerId;
    }

    public override string ToString() =>
        $"{{ message = {message}, playerId = {playerId} }}";
}
#endregion


#region Room-related responses
[Serializable]
public class AcceptRoomResponse: BaseData
{
    public string message;
    public int    playerId;
    public int    roomId;

    public AcceptRoomResponse(string message, int playerId, int roomId)
    {
        this.message  = message;
        this.playerId = playerId;
        this.roomId   = roomId;
    }

    public override string ToString() =>
        $"{{ message = {message}, playerId = {playerId}, roomId = {roomId} }}";
}

[Serializable]
public class RejectRoomResponse: BaseData
{
    public string message;
    public int    playerId;
    public int    roomId;

    public RejectRoomResponse(string message, int playerId, int roomId)
    {
        this.message  = message;
        this.playerId = playerId;
        this.roomId   = roomId;
    }

    public override string ToString() =>
        $"{{ message = {message}, playerId = {playerId}, roomId = {roomId} }}";
}
#endregion


#region Status response
[Serializable]
public class MatchmakingStatusResponse: BaseData
{
    public string  message;
    public MMRoom  room;      // Assumes you already have a serializable MMRoom class.

    public MatchmakingStatusResponse(string message, MMRoom room)
    {
        this.message = message;
        this.room    = room;
    }

    public override string ToString() =>
        $"{{ message = {message}, roomId = {room.id}, createdAt = {room.createdAt}, " +
        $"players = [{string.Join(", ", room.players)}], readyPlayers = {room.readyPlayers} }}";
}
#endregion
