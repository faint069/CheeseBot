using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public class Session : INotifyPropertyChanged
{
  private       List<IDisposable> _disposables;
  private       SessionState      _state = SessionState.Hosted;
  private       int               _rightAnswer;
  private       int               _roundCount;
  
  private const int               MaxRounds = 5;

  public Session( long hostId )
  {
    Id          = Random.Shared.NextInt64( );
    HostId      = hostId;
    TimeStarted = DateTime.Now;

    HostName = Bot.GetUserName( hostId ).Result;

    _disposables = new List<IDisposable>( );
    
    Bot.Client.SendTextMessageAsync( chatId: HostId,
                                    parseMode: ParseMode.MarkdownV2,
                                    text: $"Your new session Id is ```{Id}```" );

    var d1 = Players.Connect( )
                    .OnItemAdded( _ =>
                                  {
                                    if ( !_.IsHost )
                                    {
                                      Bot.Client.SendTextMessageAsync( text: $"New player {_.UserName} joined session.",
                                                                      chatId: HostId );
                                    }
                                  } )
                    .OnItemRemoved( _ => Bot.Client.SendTextMessageAsync( text: $"Player {_.UserName} left session",
                                                                         chatId: HostId ) )
                    .Subscribe( );


    var d2 = Players.Connect( )
                    .WhenPropertyChanged( _ => _.IsReady )
                    .Where( p => Players.Items.All( x => x.IsReady ) )
                    .Subscribe( _ => State = SessionState.GameStarted );

    var d3 = Players.Connect( )
                    .WhenPropertyChanged( _ => _.GotAnswer )
                    .Where( p => Players.Items.All( x => x.GotAnswer ) )
                    .Subscribe( async _ =>
                                {
                                  await CheckAnswers( );
                                  if ( State is not SessionState.GameEnded )
                                  {
                                    State = SessionState.RoundEnded;
                                    State = SessionState.GameStarted;
                                  }
                                } );

    var d4 = this.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value == SessionState.GameStarted )
                 .Subscribe( async _ => await StartGame( ) );

    var d5 = this.WhenPropertyChanged( _ => _.State )
                 .Where( _ => _.Value == SessionState.GameEnded )
                 .Subscribe( async _ => await SendResults( ) );

    _disposables.Add( d1 );
    _disposables.Add( d2 );
    _disposables.Add( d3 );
    _disposables.Add( d4 );
    _disposables.Add( d5 );
  }

  public long Id { get; }

  public SessionState State
  {
    get => _state;
    set => SetField( ref _state, value );
  }

  public string HostName { get; }

  public long HostId { get; }

  public SourceList<Player> Players { get; } = new( );

  public DateTime TimeStarted { get; }

  private async Task StartGame()
  {
    var trivia = GenerateTrivia( );

    var tasks = Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, "Game starts in" ) );
    await Task.WhenAll( tasks );
    tasks = Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, "3..." ) );
    await Task.WhenAll( tasks );
    await Task.Delay( 1000 );
    tasks = Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, "2..." ) );
    await Task.WhenAll( tasks );
    await Task.Delay( 1000 );
    tasks = Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, "1..." ) );
    await Task.WhenAll( tasks );
    await Task.Delay( 1000 );
    tasks = Players.Items.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, trivia ) );
    await Task.WhenAll( tasks );
  }

  private async Task CheckAnswers()
  {
    var right = Players.Items.Where( _ => _.Answer == _rightAnswer )
                       .ToList( );
    var wrong = Players.Items.Where( _ => _.Answer != _rightAnswer )
                       .ToList( );

    right.Sort( ( x, y ) => x.AnswerTime.CompareTo( y.AnswerTime ) );
    var winner = right.FirstOrDefault( );
    var tasks  = new List<Task<Message>>( );
    if ( winner is not null )
    {
      winner.WinsCount++;
      tasks.Add( wrong.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId,
                                                                    $"Winner is {winner.UserName}.\nRight answer was {_rightAnswer}" ) ) );
      tasks.Add( Bot.Client.SendTextMessageAsync( winner.TelegramId, "You are Winner! Congratulations!!!" ) );
      tasks.Add( right.Except( new List<Player> { winner } ).Select( _ =>
                                                                       Bot.Client.SendTextMessageAsync( _.TelegramId,
                                                                        $"You was right, but {winner.UserName} was faster. Sorry..." ) ) );
    }
    else
    {
      tasks.Add( wrong.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId,
                                                                    $"No one wins.\nRight answer was {_rightAnswer}" ) ) );
    }

    await Task.WhenAll( tasks );
    await Task.Delay( 2000 );

    _roundCount++;
    if ( _roundCount >= MaxRounds )
    {
      _roundCount = 0;
      State       = SessionState.GameEnded;
    }
  }

  private async Task SendResults()
  {
    var m = "Game over\n";
    var p = Players.Items.ToList(  );
    if ( p.Count == 1 )
    {
      m += $"Your score is {p.First( ).WinsCount}";
    }
    else
    {
      p.Sort( ( x, y ) => y.WinsCount.CompareTo( x.WinsCount ) );
      m +=  $"***{p.First().UserName}  {p.First( ).WinsCount}***\n";
      m += string.Join( '\n', p.Skip( 1 ).Select( _ => $"{_.UserName}  {_.WinsCount}" ) );
    }

    var tasks = p.Select( _ => Bot.Client.SendTextMessageAsync( _.TelegramId, m, ParseMode.MarkdownV2 ) );
    await Task.WhenAll( tasks );
  }
  
  private string GenerateTrivia()
  {
    var cheese = Random.Shared.Next( 1, 10 );
    var mice   = Random.Shared.Next( 0, cheese );
    var cats   = Random.Shared.Next( 0, mice );
    var dogs   = Random.Shared.Next( 0, cats );
    
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

  public override string ToString()
  {
    var sb = new StringBuilder( );
    sb.AppendLine( $"Id: {Id}" );
    sb.AppendLine( $"Hoster: {HostName}" );
    sb.AppendLine( $"State: {State}" );
    sb.AppendLine( "Players in session:");
    sb.AppendJoin( "\n", Players.Items.Select( _ => _.UserName ) );
    return sb.ToString( );
  }

  public void Dispose()
  {
    foreach ( var player in Players.Items )
    {
      player.PlayerSession = null;
    }
    
    _disposables.ForEach( _ => _.Dispose( ) );
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
