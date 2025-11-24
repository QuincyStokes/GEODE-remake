using TMPro;
using UnityEngine;
public class HostPanelUI : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private SimpleButtonGroup difficultyButtonGroup;
    [SerializeField] private SimpleButtonGroup sizeButtonGroup;

    private DifficultyButton.Difficulty selectedDifficulty;
    private WorldSizeButton.Size selectedSize;

    private void Awake()
    {
        // Subscribe to selection change events early to catch initial selection
        if (difficultyButtonGroup != null)
        {
            difficultyButtonGroup.SelectionChanged += OnDifficultySelectionChanged;
        }

        if (sizeButtonGroup != null)
        {
            sizeButtonGroup.SelectionChanged += OnSizeSelectionChanged;
        }
    }

    private void Start()
    {
        // Set initial values from the default activated buttons
        UpdateSelectedDifficulty();
        UpdateSelectedSize();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (difficultyButtonGroup != null)
        {
            difficultyButtonGroup.SelectionChanged -= OnDifficultySelectionChanged;
        }

        if (sizeButtonGroup != null)
        {
            sizeButtonGroup.SelectionChanged -= OnSizeSelectionChanged;
        }
    }

    private void OnDifficultySelectionChanged(SimpleButton button)
    {
        if (button is DifficultyButton difficultyButton)
        {
            selectedDifficulty = difficultyButton.difficulty;
        }
    }

    private void OnSizeSelectionChanged(SimpleButton button)
    {
        if (button is WorldSizeButton sizeButton)
        {
            selectedSize = sizeButton.size;
        }
    }

    private void UpdateSelectedDifficulty()
    {
        if (difficultyButtonGroup?.CurrentlyActivatedButton is DifficultyButton difficultyButton)
        {
            selectedDifficulty = difficultyButton.difficulty;
        }
    }

    private void UpdateSelectedSize()
    {
        if (sizeButtonGroup?.CurrentlyActivatedButton is WorldSizeButton sizeButton)
        {
            selectedSize = sizeButton.size;
        }
    }

    public DifficultyButton.Difficulty GetSelectedDifficulty() => selectedDifficulty;
    public WorldSizeButton.Size GetSelectedSize() => selectedSize;

    public void CreateLobbyButtonClicked()
    {
        if(string.IsNullOrEmpty(playerNameInput.text))
        {
            LobbyErrorMessages.Instance.SetError("Please enter a player name.");
        }
        else
        {
            LobbyHandler.Instance.CreateLobby(playerNameInput.text);
            MainMenuController.Instance.ShowPanel("LobbyPanel");
            //Send some info over to RunSettings (selectedDifficulty and selectedSize)
        }
        
    }

    
}
