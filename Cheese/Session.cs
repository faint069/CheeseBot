using DynamicData;
using Telegram.Bot;

namespace Cheese;

public class Session
{
  public Session( long host )
  {
    Id          = Random.Shared.NextInt64( );
    Host        = host;
    TimeStarted = DateTime.Now;

    Players.Connect( )
           .OnItemAdded( _ => Bot.Client.SendTextMessageAsync( text: $"New Player Joined Game. Id {_}",
                                                              chatId: Host ) )
           .OnItemRemoved( _ => Bot.Client.SendTextMessageAsync( text: $"Player Id {_} Left Game",
                                                                chatId: Host ) )
           .Subscribe();
  }
  
  public long Id { get; }

  public SessionState State { get; set; } = SessionState.Hosted;

  public string HostName { get; set; } = string.Empty;

  public long   Host    { get; }

  public SourceList<long> Players { get; } = new( );
  
  public DateTime TimeStarted { get; }

  public async Task StartGame()
  {
    State = SessionState.WaitingForPlayers;

    var tasks = Players.Items
                       .Select( _ => Bot.Client.SendTextMessageAsync( _, "Game will start now. Send R or Ready to get Ready" ) );

    await Task.WhenAll( tasks );
  }
}