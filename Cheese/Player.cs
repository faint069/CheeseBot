using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cheese;

public class Player : INotifyPropertyChanged
{
  private long _tId;
  private bool _isReady;

  public long TID
  {
    get => _tId;
    set => SetField( ref _tId, value );
  }
  
  public bool IsReady
  {
    get => _isReady;
    set => SetField( ref _isReady, value );
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  protected virtual void OnPropertyChanged( [ CallerMemberName ] string? propertyName = null )
  {
    PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
  }

  protected bool SetField<T>( ref T field, T value, [ CallerMemberName ] string? propertyName = null )
  {
    if ( EqualityComparer<T>.Default.Equals( field, value ) ) return false;
    field = value;
    OnPropertyChanged( propertyName );
    return true;
  }
}