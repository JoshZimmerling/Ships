using UnityEngine;

public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour 
{ 
    public static T Singleton { get; private set; }
    protected virtual void Awake() => Singleton = this as T;

    protected virtual void OnApplicationQuit()
    {
        Singleton = null;
        Destroy(gameObject);
    }
}

public abstract class Singleton<T> : StaticInstance<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Singleton != null) Destroy(gameObject);
        base.Awake();
    }
}

public abstract class PersistantSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}