using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleStartController : MonoBehaviour
{
    [SerializeField] private RunSession runSession;
    [SerializeField] private string battleSceneName;

    public RunSession RunSession => runSession;

    [ContextMenu("Start Battle Scene")]
    public void StartBattleScene()
    {
        if (runSession == null)
        {
            Debug.LogError("[BattleStartController] RunSession is required to start a battle scene.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogError("[BattleStartController] Battle scene name is required.", this);
            return;
        }

        SceneManager.LoadScene(battleSceneName);
    }
}
