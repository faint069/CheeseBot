using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public class Session : INotifyPropertyChanged
{
  private SessionState _state = SessionState.Hosted;

  public Session( long hostId )
  {
    Id          = Random.Shared.NextInt64( );
    HostId      = hostId;
    TimeStarted = DateTime.Now;

    var chatInfo = Bot.Client.GetChatAsync( HostId ).Result;
    HostName = $"{chatInfo.FirstName} {chatInfo.LastName}";

    Players.Connect( )
           .OnItemAdded( _ => Bot.Client.SendTextMessageAsync( text: $"New Player {_.UserName} Joined Game.",
                                                              chatId: HostId ) )
           .OnItemRemoved( _ => Bot.Client.SendTextMessageAsync( text: $"Player {_.UserName} Left Game",
                                                                chatId: HostId ) )
           .Subscribe( );

    Bot.Client.SendTextMessageAsync( chatId: HostId,
                                    parseMode: ParseMode.MarkdownV2,
                                    text: $"Your new session Id is ```{Id}```" );

    Players.Connect( )
           .WhenPropertyChanged( _ => _.IsReady )
           .Where( p => Players.Items.All( x => x.IsReady ) )
           .Subscribe( _ => State = SessionState.Started );
  }

  public long Id { get; }

  public SessionState State
  {
    get => _state;
    set => SetField( ref _state, value );
  }

  public string HostName { get; set; }

  public long HostId { get; }

  public SourceList<Player> Players { get; } = new( );

  public DateTime TimeStarted { get; }
  
  public void ProcessMessage( string messageText )
  {
    if ( messageText.StartsWith( "/Start" ) || messageText.StartsWith( "/s" ) )
    {
      State = SessionState.WaitingForPlayers;
    }
  }
  
  #region INPC

  public event PropertyChangedEventHandler? PropertyChanged;

  protected virtual void OnPropertyChanged( [ CallerMemberName ] string? propertyName = null )
  {
    PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
  }

  protected bool SetField<T>( ref T field, T value, [ CallerMemberName ] string? propertyName = null )
  {
    if ( EqualityComparer<T>.Default.Equals( field, value ) ) return false;
    field = value;
    OnPropertyChanged( propertyName );
    return true;
  }

  #endregion
}
