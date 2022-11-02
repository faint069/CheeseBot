using Telegram.Bot;

namespace Cheese.Dialogs;

public class JoinGameDialog : IDialog
{
    private ITelegramBotClient _client;
    
    public JoinGameDialog(ITelegramBotClient client, long chatId)
    {
        _client = client;
        ChatId = chatId;
    }
    
    public long ChatId { get; }
    
    public int Step { get; set; } = 0;
    
    public bool IsOver { get; set; }
    
    public int MaxSteps { get; } = 2;
    
    public long SelectedSessionId { get; set; }

    public Task PerformStep()
    {
        return Task.CompletedTask;
    }

    public async Task PerformStep(string text)
    {
        switch (Step)
        {
            case 0:
            {
                var sentMessage = await _client.SendTextMessageAsync(chatId: ChatId,
                    text: "Provide SessionId");
                Step++;
                break;
            }
            case 1:
                if (long.TryParse(text, out var selected))
                {
                    SelectedSessionId = selected;
                    IsOver = true;
                }
                else
                {
                    var sentMessage = await _client.SendTextMessageAsync(chatId: ChatId,
                        text: "Wrong SessionId. Try Again");
                }
                break;
        }
    }
}