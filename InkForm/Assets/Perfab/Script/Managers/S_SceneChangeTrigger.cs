using UnityEngine;
using UnityEngine.SceneManagement;

public class S_SceneChangeTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private S_SceneReference targetScene = new S_SceneReference();
    [SerializeField, HideInInspector] private string sceneName;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetScene == null)
            targetScene = new S_SceneReference();

        targetScene.EditorSyncAsset();
        string key = targetScene.IsValid ? targetScene.RuntimeKey : sceneName;
        if (!string.IsNullOrWhiteSpace(key))
            targetScene.EditorTryAssignByKey(key);

        if (targetScene.IsValid && !targetScene.EditorIsInEnabledBuildScenes())
            Debug.LogWarning($"[SceneChangeTrigger] Target scene '{targetScene.RuntimeKey}' is not enabled in File > Build Profiles / Build Settings.", this);
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        string sceneKey = targetScene != null && targetScene.IsValid ? targetScene.RuntimeKey : sceneName;
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("Target scene is empty on " + gameObject.name);
            return;
        }

        if (S_GameManager.Instance != null)
        {
            S_GameEvent.SceneLoadRequested(sceneKey);
            return;
        }

        if (!S_SceneReference.CanLoadScene(sceneKey))
        {
            Debug.LogError($"[SceneChangeTrigger] Scene '{sceneKey}' cannot be loaded. Drag a valid scene asset and make sure it is enabled in File > Build Profiles / Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneKey);
    }
}
