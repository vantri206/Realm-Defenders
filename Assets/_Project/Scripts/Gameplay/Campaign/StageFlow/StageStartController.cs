using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageStartController : MonoBehaviour
{
    [SerializeField] private PlayerSession playerSession;
    [SerializeField] private string stageSceneName;
    [SerializeField] private QuestStageView questStageView;
    [SerializeField] private Button playButton;

    private CombatStageDefinition selectedStage;

    public PlayerSession PlayerSession => playerSession;

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    [ContextMenu("Start Stage")]
    public void StartStage()
    {
        if (playerSession == null)
        {
            Debug.LogError("[StageStartController] PlayerSession is required to start a stage.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(stageSceneName))
        {
            Debug.LogError("[StageStartController] Stage scene name is required.", this);
            return;
        }

        if (selectedStage == null && questStageView != null)
        {
            selectedStage = questStageView.SelectedStage;
        }

        if (selectedStage == null)
        {
            Debug.LogError("[StageStartController] A stage must be selected before starting combat.", this);
            return;
        }

        playerSession.SetSelectedStage(selectedStage);
        SceneManager.LoadScene(stageSceneName);
    }

    private void HandleStageSelected(CombatStageDefinition stageDefinition)
    {
        selectedStage = stageDefinition;
    }

    private void HandlePlayButtonClicked()
    {
        GameAudioManager audioManager = GameAudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayUIButtonClick();
        }

        StartStage();
    }

    private void RegisterEvents()
    {
        if (questStageView != null)
        {
            questStageView.OnStageSelected -= HandleStageSelected;
            questStageView.OnStageSelected += HandleStageSelected;
            selectedStage = questStageView.SelectedStage;
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            playButton.onClick.AddListener(HandlePlayButtonClicked);
        }
    }

    private void UnregisterEvents()
    {
        if (questStageView != null)
        {
            questStageView.OnStageSelected -= HandleStageSelected;
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayButtonClicked);
        }
    }
}
