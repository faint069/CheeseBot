using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using DynamicData.Binding;
using Telegram.Bot;

namespace Cheese;

public class Player : INotifyPropertyChanged
{
  private bool              _isReady;
  private bool              _gotAnswer;
  private List<IDisposable> _disposables;

  public Player( long telegramId )
  {
    TelegramId = telegramId;
    UserName   = Bot.GetUserName( telegramId ).Result;

    _disposables = new List<IDisposable>( );
  }
  
  public string UserName { get; }

  public long TelegramId { get; }

  public bool IsHost { get; set; }

  public DateTime AnswerTime { get; set; }
  
  public Session? PlayerSession { get; set; }
  
  public int Answer { get; set; }
  
  public int WinsCount { get; set; }
  
  public bool IsReady
  {
    get => _isReady;
    set => SetField( ref _isReady, value );
  }

  public bool GotAnswer
  {
    get => _gotAnswer;
    set => SetField( ref _gotAnswer, value);
  }

  public void AddSession( Session session )
  {
    session.Players.Add( this );
    PlayerSession = session;
    
    var d1 = PlayerSession.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value == SessionState.WaitingForPlayers )
                 .Subscribe( _ =>
                             {
                               if ( !IsHost )
                               {
                                 Bot.Client.SendTextMessageAsync( TelegramId,
                                                                 "Session is ready to begin. Send any text message to get ready" );
                               }
                             } ); 
    
    var d2 = PlayerSession.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value is SessionState.RoundEnded  )
                 .Subscribe( _ =>
                             {
                               Answer    = 0;
                               GotAnswer = false;
                             } );    
    
    var d3 = PlayerSession.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value is SessionState.GameEnded  )
                 .Subscribe( _ =>
                             {
                               IsReady   = false;
                               Answer    = 0;
                               GotAnswer = false;
                               WinsCount = 0;
                             } );
    
    _disposables.Add( d1 );
    _disposables.Add( d2 );
    _disposables.Add( d3 );
  }

  public void RemoveSession()
  {
    _disposables.ForEach( _ => _.Dispose(  ) );
    _disposables.Clear(  );
    PlayerSession = null;
  }
  
  public async Task ProcessMessage( string messageText )
  {
    if ( messageText == "/rules" )
    {
      var m =
        "It is a simple game.\nAt first you should host a game. You will receive a session id. "            +
        "Other players shod start bot and send this id to join your session. Then you can start a game.\n"  +
        "In game you will receive puzzle, containing dogs, cat, mice and cheese. "                          +
        "Your goal is to cont how much cheese will left. Dog kicks cat, cat kicks mouse, mouse eat cheese " +
        "and you should tell how much cheese is left.\nThat's all";
    }
    else if ( messageText == "/status" )
    {
      string r;
      if ( PlayerSession is null )
      {
        r = "You are not in session";
      }
      else
      {
        r = $"You are in session {PlayerSession}";
      }

      await Bot.Client.SendTextMessageAsync( TelegramId, r );
    }
    else if ( messageText == "/leave" )
    {
      if ( IsHost )
      {
        var tasks =
          PlayerSession.Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId,
                                               $"Session {PlayerSession.Id} is closed. by hoster" ) );
        await Task.WhenAll( tasks );
        DataStore.RemoveSessionById( PlayerSession.Id );
      }
      else
      {
        PlayerSession?.Players.Remove( this );
        RemoveSession( );
        await Bot.Client.SendTextMessageAsync( TelegramId, $"You left session {PlayerSession?.Id}" );
      }
    }
    else if ( PlayerSession is null )
    {
      if ( messageText == "/join" )
      {
        await Bot.Client.SendTextMessageAsync( TelegramId, $"Provide Session Id" );
      }
      else if (long.TryParse(messageText, out var sessionId))
      {
        if ( DataStore.CheckIfSessionExist( sessionId ) )
        {
          AddSession( DataStore.GetSessionById( sessionId ) );
        }
        else
        {
          await Bot.Client.SendTextMessageAsync( TelegramId, "Session with provided Id doesn't exist. Try another" );
        }
      }
      else if ( messageText == "/host" )
      {
        DataStore.AddNewSession( this );
      }
      else
      {
        await Bot.Client.SendTextMessageAsync( TelegramId, $"You must host or join game first. Use commands" );
      }
    }
    else
    {
      if ( messageText is "/join" or "/host" )
      {
        if ( IsHost )
        {
          await Bot.Client.SendTextMessageAsync( TelegramId,
                                                $"Currently you are hosting session {PlayerSession.Id}. Leave it first" );
        }
        else
        {
          await Bot.Client.SendTextMessageAsync( TelegramId,
                                                $"You are now in session {PlayerSession.Id}. Leave it first" );
        }
      }
      if ( PlayerSession.State is SessionState.WaitingForPlayers )
      {
        IsReady = true;
      }

      else if (PlayerSession.State is SessionState.GameStarted)
      {
        if (int.TryParse(messageText, out var i))
        {
          Answer     = i;
          AnswerTime = DateTime.Now;
          GotAnswer  = true;
        }
        else
        {
          await Bot.Client.SendTextMessageAsync(TelegramId, "Please, send valid number");
        }
      }
    
      else if ( IsHost                                                                   &&
                ( PlayerSession.State is SessionState.Hosted or SessionState.GameEnded ) &&
                ( messageText.StartsWith( "/start" ) || messageText.StartsWith( "/s" ) ) )
      {
        PlayerSession.State = SessionState.WaitingForPlayers;
        IsReady             = true;
      }
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