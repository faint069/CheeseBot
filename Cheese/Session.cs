using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using DynamicData.Binding;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public class Session : INotifyPropertyChanged
{
  private SessionState _state = SessionState.Hosted;
  private int _rightAnswer;
  
  public Session( long hostId )
  {
    Id          = Random.Shared.NextInt64( );
    HostId      = hostId;
    TimeStarted = DateTime.Now;
    
    HostName = Bot.GetUserName( hostId ).Result;

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
           .Subscribe( _ => State = SessionState.GameStarted );
    
    Players.Connect( )
           .WhenPropertyChanged( _ => _.GotAnswer )
           .Where( p => Players.Items.All( x => x.GotAnswer ) )
           .Subscribe( _ => CheckAnswers() );

    this.WhenPropertyChanged( _ => _.State )
        .Where( _ => _.Value == SessionState.GameStarted )
        .Subscribe(async _ =>
        {
          await StartGame();
        } );
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
    if ( State is SessionState.Hosted &&
         ( messageText.StartsWith( "/Start" ) || messageText.StartsWith( "/s" ) ) )
    {
      State = SessionState.WaitingForPlayers;
    }
  }

  private async Task StartGame()
  {
    var trivia = GenerateTrivia();

    var playersIds = Players.Items.Select(_ => _.TelegramId)
      .ToList();
    playersIds.Add(HostId);

    var tasks = playersIds.Select(_ => Bot.Client.SendTextMessageAsync(_, "Game starts in"));
    await Task.WhenAll(tasks);
    tasks = playersIds.Select(_ => Bot.Client.SendTextMessageAsync(_, "3..."));
    await Task.WhenAll(tasks);
    await Task.Delay(1000);
    tasks = playersIds.Select(_ => Bot.Client.SendTextMessageAsync(_, "2..."));
    await Task.WhenAll(tasks);
    await Task.Delay(1000);
    tasks = playersIds.Select(_ => Bot.Client.SendTextMessageAsync(_, "1..."));
    await Task.WhenAll(tasks);
    await Task.Delay(1000);
    tasks = playersIds.Select(_ => Bot.Client.SendTextMessageAsync(_, trivia));
    await Task.WhenAll(tasks);
  }
  
  private void CheckAnswers()
  {
    var right = Players.Items.Where(_ => _.Answer == _rightAnswer)
      .ToList();
    var wrong = Players.Items.Where(_ => _.Answer != _rightAnswer)
      .ToList();

    right.Sort((x, y) => x.AnswerTime.CompareTo(y.AnswerTime));
    var winner = right.FirstOrDefault();
    var tasks = new List<Task<Message>>();
    if (winner is not null)
    {
      tasks.Add(wrong.Select(_ => Bot.Client.SendTextMessageAsync(_.TelegramId,
        $"Winner is {winner.UserName}.\nRight answer was {_rightAnswer}")));
      tasks.Add(Bot.Client.SendTextMessageAsync(winner.TelegramId, "You are Winner! Congratulations!!!"));
      tasks.Add(right.Except(new List<Player> {winner}).Select(_ =>
        Bot.Client.SendTextMessageAsync(_.TelegramId, $"You was right, but {winner.UserName} was faster. Sorry...")));
    }
    else
    {
      tasks.Add(wrong.Select(_ => Bot.Client.SendTextMessageAsync(_.TelegramId,
        $"No one wins.\nRight answer was {_rightAnswer}")));
    }
  }

  
  private string GenerateTrivia()
  {
    var dogs   = Random.Shared.Next( 0, 10 );
    var cats   = Random.Shared.Next( 0, 10 );
    var mice   = Random.Shared.Next( 0, 10 );
    var cheese = Random.Shared.Next( 0, 10 );
    
    _rightAnswer = cheese - ( mice - ( cats - dogs ) );
    
    var str    = new List<string>( );
    str.AddRange( Enumerable.Repeat( "🐶", dogs ) );
    str.AddRange( Enumerable.Repeat( "🐱", cats ) );
    str.AddRange( Enumerable.Repeat( "🐭", mice ) );
    str.AddRange( Enumerable.Repeat( "🧀", cheese ) );
    
    var n = str.Count;  
    while (n > 1) 
    {  
      n--;  
      var k     = Random.Shared.Next(n + 1);  
      ( str[k], str[n] ) = ( str[n], str[k] );
    }

    return string.Join( "", str );  
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
