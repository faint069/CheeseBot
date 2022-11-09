using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public static class MessageHandler
{
  public static async Task HandleUpdateAsync( ITelegramBotClient botClient, 
                                              Update update,
                                              CancellationToken  cancellationToken )
  {
    if ( update.Message is not { } message )
      return;

    if ( message.Text is not { } messageText )
      return;

    var chatId = message.Chat.Id;

    Console.WriteLine( $"Received a '{messageText}' message in chat {chatId}." );

    if ( message.Text == "/list" )
    {
      await botClient.SendTextMessageAsync( chatId: chatId,
                                           parseMode: ParseMode.MarkdownV2,
                                           text:
                                           $"Available sessions:\n{DataStore.SessionsWithUserNames}",
                                           cancellationToken: cancellationToken );
    }
    else
    {
      await DataStore.GetOrAddPlayerById( chatId ).ProcessMessage( messageText );
    }
  }
}