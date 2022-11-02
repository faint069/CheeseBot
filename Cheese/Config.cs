using Microsoft.Extensions.Configuration;

namespace Cheese
{
    public static class Config
    {
        private static readonly string _telegram_token;

        static Config()
        {
            IConfigurationRoot config_builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _telegram_token = config_builder.GetSection("Authorization")
                .GetSection("BotToken")
                .Get<string>()!;
        }

        public static string TelegramToken => _telegram_token;
    }
}