using UnityEngine;
using UnityEngine.SceneManagement;

public class StageStartController : MonoBehaviour
{
    [SerializeField] private PlayerSession playerSession;
    [SerializeField] private string stageSceneName;

    public PlayerSession PlayerSession => playerSession;

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

        SceneManager.LoadScene(stageSceneName);
    }
}
