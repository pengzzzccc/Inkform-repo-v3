using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Collectible key item. Player touches it to collect.
/// Keys persist across deaths (not reset on respawn).
/// Resets automatically when a new scene loads.
/// </summary>
public class S_Key : MonoBehaviour
{
    private static readonly HashSet<S_Key> allKeys = new HashSet<S_Key>();
    private static int collectedCount;
    private static bool sceneHooked;

    public static int TotalKeys => allKeys.Count;
    public static int CollectedKeys => collectedCount;

    private bool isCollected;

    private void Awake()
    {
        if (!sceneHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHooked = true;
        }

        allKeys.Add(this);
        isCollected = false;
    }

    private void OnDestroy()
    {
        allKeys.Remove(this);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // New level loaded — reset collection state
        // (allKeys will be repopulated by new scene's S_Key Awake calls)
        collectedCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;
        if (!collision.CompareTag("Player")) return;

        Collect();
    }

    private void Collect()
    {
        isCollected = true;
        collectedCount++;

        gameObject.SetActive(false);

        S_GameEvent.KeyCollected();
        S_GameEvent.KeyCountChanged(collectedCount, allKeys.Count);
    }
}