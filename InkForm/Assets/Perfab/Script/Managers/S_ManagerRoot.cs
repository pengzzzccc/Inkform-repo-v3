using UnityEngine;

[DisallowMultipleComponent]
public class S_ManagerRoot : MonoBehaviour
{
    private const string RootName = "PersistentManagers";

    private static bool isShuttingDown;

    public static S_ManagerRoot Instance { get; private set; }
    public static bool IsShuttingDown => isShuttingDown;
    public static bool CanCreateRuntimeRoot => Application.isPlaying && !isShuttingDown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        isShuttingDown = false;
    }

    public static S_ManagerRoot EnsureExists()
    {
        if (Instance != null)
            return Instance;

        if (isShuttingDown)
            return null;

        S_ManagerRoot existingRoot = FindAnyObjectByType<S_ManagerRoot>();
        if (existingRoot != null)
        {
            Instance = existingRoot;
            if (Application.isPlaying)
                existingRoot.PreserveRoot();
            return existingRoot;
        }

        if (!CanCreateRuntimeRoot)
            return null;

        GameObject rootObject = new GameObject(RootName);
        return rootObject.AddComponent<S_ManagerRoot>();
    }

    public static void AttachPersistent(Transform target)
    {
        if (target == null)
            return;

        S_ManagerRoot root = EnsureExists();
        if (root == null)
            return;

        if (target == root.transform || target.parent == root.transform)
            return;

        target.SetParent(root.transform, true);
    }

    public static void DestroyDuplicate(MonoBehaviour component)
    {
        if (component == null)
            return;

        GameObject targetObject = component.gameObject;
        Component[] components = targetObject.GetComponents<Component>();
        bool onlyThisComponent = components.Length <= 2;
        bool hasChildren = targetObject.transform.childCount > 0;

        if (onlyThisComponent && !hasChildren)
            Destroy(targetObject);
        else
            Destroy(component);
    }

    public Transform GetOrCreateChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            childName = "Manager";

        Transform existingChild = transform.Find(childName);
        if (existingChild != null)
            return existingChild;

        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        return child.transform;
    }

    public T GetOrCreateComponent<T>(string childName) where T : Component
    {
        Transform child = GetOrCreateChild(childName);
        T component = child.GetComponent<T>();
        return component != null ? component : child.gameObject.AddComponent<T>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
            PreserveRoot();
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (Application.isPlaying)
                isShuttingDown = true;

            Instance = null;
        }
    }

    private void PreserveRoot()
    {
        gameObject.name = RootName;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
    }
}
