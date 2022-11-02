using Telegram.Bot;
using Telegram.Bot.Types;

namespace Cheese;

public static class MessageHandler
{
  private static Dictionary<long, Session> _sessions = new( );

  public static async Task HandleUpdateAsync( ITelegramBotClient botClient, Update update,
                                              CancellationToken  cancellationToken )
  {
    if ( update.Message is not { } message )
      return;

    if ( message.Text is not { } messageText )
      return;

    var chatId = message.Chat.Id;

    Console.WriteLine( $"Received a '{messageText}' message in chat {chatId}." );

    if ( message.Text == "/HostGame" )
    {
      var ret = HostNewGame( chatId );

      var sentMessage = await botClient.SendTextMessageAsync( chatId: chatId,
                                                             text: $"Your new session Id is {ret}",
                                                             cancellationToken: cancellationToken );
    }

    if ( message.Text == "/ListGames" )
    {
      var sentMessage = await botClient.SendTextMessageAsync( chatId: chatId,
                                                             text: $"Available sessions: \n {string.Join('\n', _sessions.Keys)}",
                                                             cancellationToken: cancellationToken );
    }
  }

  private static long HostNewGame( long chatId )
  {
    var s = new Session( chatId );
    _sessions.Add( s.Id, s );
    
    return s.Id;
  }
}