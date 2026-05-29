using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    [FormerlySerializedAs("levelText")]
    [SerializeField] private TMP_Text _levelText;
    [FormerlySerializedAs("winPanel")]
    [SerializeField] private GameObject _winPanel;
    [FormerlySerializedAs("failPanel")]
    [SerializeField] private GameObject _failPanel;
    [FormerlySerializedAs("continueButton")]
    [SerializeField] private Button _continueButton;
    [FormerlySerializedAs("retryButton")]
    [SerializeField] private Button _retryButton;
    [FormerlySerializedAs("winRetryButton")]
    [SerializeField] private Button _winRetryButton;
    [FormerlySerializedAs("debugUILogs")]
    [SerializeField] private bool _debugUiLogs;

    private void Awake()
    {
        if (_winPanel == null)
        {
            Debug.LogWarning("UIManager: winPanel is not assigned.");
        }

        if (_failPanel == null)
        {
            Debug.LogWarning("UIManager: failPanel is not assigned.");
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        WireRetryButton(_retryButton);
        WireRetryButton(_winRetryButton);

        HidePanels();
    }

    private void OnEnable()
    {
        GameplayEvents.LevelStarting += HidePanels;
        GameplayEvents.LevelDisplayChanged += SetLevelDisplayText;
        GameplayEvents.GameWon += ShowWin;
        GameplayEvents.GameFailed += ShowFail;
    }

    private void OnDisable()
    {
        GameplayEvents.LevelStarting -= HidePanels;
        GameplayEvents.LevelDisplayChanged -= SetLevelDisplayText;
        GameplayEvents.GameWon -= ShowWin;
        GameplayEvents.GameFailed -= ShowFail;
    }

    public void SetLevelDisplayText(string displayText)
    {
        if (_levelText != null)
        {
            _levelText.text = displayText ?? string.Empty;
        }
    }

    public void SetLevelText(int levelNumber)
    {
        SetLevelDisplayText($"Level {levelNumber}");
    }

    public void ShowWin()
    {
        if (_debugUiLogs)
        {
            Debug.Log($"UIManager.ShowWin | winPanel={(_winPanel != null)} failPanel={(_failPanel != null)}");
        }

        if (_winPanel != null)
        {
            _winPanel.SetActive(true);
        }

        if (_failPanel != null)
        {
            _failPanel.SetActive(false);
        }
    }

    public void ShowFail()
    {
        if (_debugUiLogs)
        {
            Debug.Log($"UIManager.ShowFail | failPanel={(_failPanel != null)} winPanel={(_winPanel != null)}");
        }

        if (_failPanel != null)
        {
            _failPanel.SetActive(true);
        }

        if (_winPanel != null)
        {
            _winPanel.SetActive(false);
        }
    }

    private void WireRetryButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnRetryButtonClicked);
    }

    public void HidePanels()
    {
        if (_winPanel != null)
        {
            _winPanel.SetActive(false);
        }

        if (_failPanel != null)
        {
            _failPanel.SetActive(false);
        }
    }

    private void OnContinueButtonClicked()
    {
        GameplayEvents.RaiseContinueRequested();
    }

    private void OnRetryButtonClicked()
    {
        GameplayEvents.RaiseRetryRequested();
    }
}
