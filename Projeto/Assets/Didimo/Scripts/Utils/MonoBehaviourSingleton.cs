using UnityEngine;

namespace Didimo.Utils
{
    /// <summary>
    /// An abstract class to derive from to create monobehaviour singletons.
    /// Declare your singleton class like this:
    /// 
    /// 'public class MyClassName : MonoBehaviourSingleton<MyClassName>{...'
    /// 
    /// You will then access your classes properties like so: 'MyClassName.Instance.MethodName()'
    /// </summary>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour
    where T : MonoBehaviourSingleton<T>, new()
    {
        private static T _instance;

        private static object _lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return default(T);
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {

                        T[] res = (T[])Resources.FindObjectsOfTypeAll(typeof(T));

                        if (res.Length != 0)
                        {
                            _instance = res[0];
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            singleton.hideFlags = HideFlags.HideAndDontSave;
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

#if !UNITY_EDITOR
                            DontDestroyOnLoad(_instance);
#endif

                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool applicationIsQuitting = false;

        /// <summary>
        /// When unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public void OnApplicationQuit()
        {
            applicationIsQuitting = true;
            if (_instance != null && _instance.gameObject != null)
            {
                DestroyImmediate(_instance.gameObject);
            }
        }

    }
}