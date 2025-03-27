namespace Zypher.Services;

public class PopupStateService
{
    public event Action OnStateChanged;
    
    private bool _isPopupActive;
    public bool IsPopupActive
    {
        get => _isPopupActive;
        set
        {
            if (_isPopupActive != value)
            {
                _isPopupActive = value;
                OnStateChanged?.Invoke();
            }
        }
    }
}
