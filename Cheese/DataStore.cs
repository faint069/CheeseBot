namespace Cheese;

public static class DataStore
{
  static DataStore()
  {
    Sessions = new Dictionary<long, Session>( );
    Players  = new Dictionary<long, Player>( );
  }

  private static Dictionary<long, Session> Sessions { get; }

  private static Dictionary<long, Player> Players { get; }

  public static bool AddNewSession( Player hoster )
  {
    var s = new Session( hoster.TelegramId );
    hoster.IsHost = true;
    hoster.AddSession( s );
    Sessions.Add( s.Id, s );

    return true;
  }

  public static bool AddNewPlayer( long chatId )
  {
    Players.Add( chatId, new Player( chatId ) );
    return true;
  }

  public static string SessionsWithUserNames =>
    string.Join( '\n', Sessions.Values.Select( _ => $"```{_.Id}``` {_.HostName}" ) );

  public static bool CheckIfPlayerExist( long id ) => Players.ContainsKey( id );

  public static bool CheckIfSessionExist( long id ) => Sessions.ContainsKey( id );

  public static Player GetOrAddPlayerById( long id )
  {
    var exist = Players.TryGetValue( id, out var p );
    if ( !exist )
    {
      p = new Player( id );
      Players.Add( id, p );
    }

    return p;
  }

  public static Session GetSessionById( long id ) => Sessions[id];

  public static bool RemoveSessionById( long id )
  {
    Sessions[id].Dispose( );
    return Sessions.Remove( id );
  }
}