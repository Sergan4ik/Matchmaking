public class MMRoom
{
    public int id;
    public int createdAt;
    public List<SBMMPlayerInfo> players = new List<SBMMPlayerInfo>();
    public ConcurrentBitset readyPlayers;
}