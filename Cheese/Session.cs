namespace Cheese;

public class Session
{
  public Session( long host )
  {
    Id   = Random.Shared.NextInt64( );
    Host = host;
  }

  public long Id { get; }
  
  public string HostName { get; set; }

  public long   Host    { get; }

  public List<long> Players { get; } = new List<long>();
}