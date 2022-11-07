using Telegram.Bot;

namespace Cheese;

public static class Bot
{
  public static ITelegramBotClient Client;

  public static void Connect( string token )
  {
    Client = new TelegramBotClient( token );
  }

  public static async Task<string> GetUserName( long chatId )
  {
    var chatInfo = await Client.GetChatAsync( chatId );

    var ret = string.Empty;

    if ( !string.IsNullOrWhiteSpace( chatInfo.FirstName ) )
    {
      ret += chatInfo.FirstName;
    }

    if ( !string.IsNullOrWhiteSpace( chatInfo.LastName ) )
    {
      ret += chatInfo.LastName;
    }

    if ( string.IsNullOrWhiteSpace( ret ) )
    {
      ret = chatInfo.Username;
    }

    return ret;
  }
}