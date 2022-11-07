using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData.Binding;
using Telegram.Bot;

namespace Cheese;

public class Player : INotifyPropertyChanged
{
  private bool _isReady;

  public Player( long telegramId, Session session )
  {
    TelegramId    = telegramId;
    PlayerSession = session;

    UserName = Bot.GetUserName( telegramId ).Result;

    PlayerSession.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value == SessionState.WaitingForPlayers )
                 .Subscribe( _ => Bot.Client.SendTextMessageAsync( TelegramId,
                                                                  "Session is ready to begin. Send R or Ready to start" ) );
  }

  public string UserName { get; }

  public long TelegramId { get; }

  public bool IsReady
  {
    get => _isReady;
    set => SetField( ref _isReady, value );
  }

  public Session PlayerSession { get; }

  public void ProcessMessage( string messageText )
  {
    if ( PlayerSession.State is SessionState.WaitingForPlayers &&
         ( messageText.StartsWith( "R" ) || messageText.StartsWith( "Ready" ) ) )
    {
      IsReady = true;
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