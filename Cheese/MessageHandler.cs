using Cheese.Dialogs;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cheese;

public static class MessageHandler
{
  private static Dictionary<long, Session> _sessions = new();
  private static Dictionary<long, JoinGameDialog> _joinGameDialogs = new();

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
        _sessions[dialog.SelectedSessionId].Players.Add(chatId);
        _joinGameDialogs.Remove(chatId);
        var sentMessage = await botClient.SendTextMessageAsync(chatId: chatId,
          parseMode: ParseMode.MarkdownV2,
          text: $" You was added to session```{dialog.SelectedSessionId}```",
          cancellationToken: cancellationToken);
      }
    }
    
    if (message.Text == "/HostGame" || message.Text == "/hg")
    {
      var s = new Session(chatId);
      var chatInfo = await botClient.GetChatAsync(chatId, cancellationToken);
      s.HostName = $"{chatInfo.FirstName} {chatInfo.LastName}";

      _sessions.Add(s.Id, s);

      var sentMessage = await botClient.SendTextMessageAsync(chatId: chatId,
        parseMode: ParseMode.MarkdownV2,
        text: $"Your new session Id is ```{s.Id}```",
        cancellationToken: cancellationToken);
    }

    if (message.Text.StartsWith("/JoinGame"))
    {
      if (_joinGameDialogs.ContainsKey(chatId))
      {
        _joinGameDialogs.Remove(chatId);
      }
      var newDialog = new JoinGameDialog(botClient, chatId);
        _joinGameDialogs.Add(chatId, newDialog);
        await newDialog.PerformStep(string.Empty);
    }

    if (message.Text == "/ListGames")
    {
      var sentMessage = await botClient.SendTextMessageAsync(chatId: chatId,
        parseMode: ParseMode.MarkdownV2,
        text: $"Available sessions:\n{string.Join('\n', _sessions.Values.Select(_ => $"```{_.Id}``` {_.HostName}"))}",
        cancellationToken: cancellationToken);
    }
  }
}