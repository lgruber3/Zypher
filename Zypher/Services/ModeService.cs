namespace Zypher.Services;

public class ModeService
{
    private TextType _selectedMode = TextType.Words; 
    
    public event Action OnModeChanged;

    public TextType SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode != value)
            {
                _selectedMode = value;
                OnModeChanged?.Invoke();
            }
        }
    }
}
