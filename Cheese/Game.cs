namespace Cheese;

public class Game
{
  private Session      _session;
  private List<Player> _players;
  
  public Game( Session session )
  {
    _session = session;
    _players = new List<Player>( );
    _players.AddRange( session.Players.Items );
    _players.Add( new Player( session.HostId, session ) );
  }

  public string Round()
  {
    var dogs   = Random.Shared.Next( 0, 10 );
    var cats   = Random.Shared.Next( 0, 10 );
    var mice   = Random.Shared.Next( 0, 10 );
    var cheese = Random.Shared.Next( 0, 10 );
    
    var answer = cheese - ( mice - ( cats - dogs ) );
    
    var str    = new List<char>( );
    str.AddRange( Enumerable.Repeat( 'D', dogs ) );
    str.AddRange( Enumerable.Repeat( 'C', cats ) );
    str.AddRange( Enumerable.Repeat( 'M', mice ) );
    str.AddRange( Enumerable.Repeat( 'S', cheese ) );
    
    var n = str.Count;  
    while (n > 1) 
    {  
      n--;  
      var k     = Random.Shared.Next(n + 1);  
      ( str[k], str[n] ) = ( str[n], str[k] );
    }  

    return string.Join( "", str );
  }
}