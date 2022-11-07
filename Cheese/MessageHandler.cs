using Cheese.Dialogs;
using DynamicData;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public static class MessageHandler
{
  private static Dictionary<long, Session> _sessions = new();
  private static Dictionary<long, JoinGameDialog> _joinGameDialogs = new();

  private static Dictionary<long, Session> _hosters = new();
  private static Dictionary<long, Player>  _players = new( );
  
  public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
    CancellationToken cancellationToken)
  {
    if (update.Message is not { } message)
      return;

    if (message.Text is not { } messageText)
      return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    
    if (_joinGameDialogs.ContainsKey(chatId))
    {
      var dialog = _joinGameDialogs[chatId];
      await dialog.PerformStep(messageText);
      if (dialog.IsOver)
      {
        var p = new Player( chatId, _sessions[dialog.SelectedSessionId] );
        _sessions[dialog.SelectedSessionId].Players.Add( p );
        _joinGameDialogs.Remove(chatId);
        _players.Add( chatId, p );
        var sentMessage = await botClient.SendTextMessageAsync(chatId: chatId,
          parseMode: ParseMode.MarkdownV2,
          text: $" You was added to session```{dialog.SelectedSessionId}```",
          cancellationToken: cancellationToken);
      }
    }
    
    else if (message.Text == "/HostGame" || message.Text == "/hg")
    {
      var s = new Session(chatId);
      
      _sessions.Add(s.Id, s);
      _hosters.Add(chatId, s);
    }

    else if (message.Text.StartsWith("/JoinGame"))
    {
      if (_joinGameDialogs.ContainsKey(chatId))
      {
        _joinGameDialogs.Remove(chatId);
      }
      var newDialog = new JoinGameDialog(botClient, chatId);
        _joinGameDialogs.Add(chatId, newDialog);
        await newDialog.PerformStep(string.Empty);
    }

    else if (message.Text == "/ListGames")
    {
      var sentMessage = await botClient.SendTextMessageAsync(chatId: chatId,
        parseMode: ParseMode.MarkdownV2,
        text: $"Available sessions:\n{string.Join('\n', _sessions.Values.Select(_ => $"```{_.Id}``` {_.HostName}"))}",
        cancellationToken: cancellationToken);
    }
    
    else if ( _hosters.ContainsKey( chatId ) )
    {
      _hosters[chatId].ProcessMessage( messageText );
    }
    
    if ( _players.ContainsKey( chatId ) )
    {
      _players[chatId].ProcessMessage( messageText );
    }
  }
}