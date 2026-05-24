using UnityEngine;
using UnityEngine.SceneManagement;

public class S_SceneChangeTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneName;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty on " + gameObject.name);
            return;
        }

        if (S_GameManager.Instance != null)
            S_GameEvent.SceneLoadRequested(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
