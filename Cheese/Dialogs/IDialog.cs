namespace Cheese.Dialogs;

public interface IDialog
{
  public long ChatId { get; }
  public int Step { get; set; }
  public bool IsOver { get; set; }
  public int MaxSteps { get; }

  public Task PerformStep( );
  public Task PerformStep( string text );

}