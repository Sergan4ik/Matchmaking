using ZergRush.CodeGen;

[Serializable]
public class MMRoom : BaseData
{
    public int id;
    public int createdAt;
    public List<SBMMPlayerInfo> players = new List<SBMMPlayerInfo>();
    public ConcurrentBitset readyPlayers;
    public RoomReleaseReason releaseReason = RoomReleaseReason.NotReleased;
}

[GenTask(GenTaskFlags.PolymorphicDataPack), GenInLocalFolder]
public class BaseData
{
    
}

public enum RoomReleaseReason
{
    NotReleased,
    RoomAccepted,
    RoomTimeout,
    RoomCancelled,
}
