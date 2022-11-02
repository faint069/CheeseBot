using Cheese;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient(Config.TelegramToken);

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
                      {
                        AllowedUpdates = Array.Empty<UpdateType>()
                      };
botClient.StartReceiving( updateHandler: MessageHandler.HandleUpdateAsync,
                         pollingErrorHandler: HandlePollingErrorAsync,
                         receiverOptions: receiverOptions,
                         cancellationToken: cts.Token
                        );

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();




Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
  var ErrorMessage = exception switch
                     {
                       ApiRequestException apiRequestException
                         => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                       _ => exception.ToString()
                     };

  Console.WriteLine(ErrorMessage);
  return Task.CompletedTask;
}