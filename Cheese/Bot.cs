using Telegram.Bot;

namespace Cheese;

public static class Bot
{
  public static ITelegramBotClient Client;

  public static void Connect( string token )
  {
    Client = new TelegramBotClient( token );
  }
}