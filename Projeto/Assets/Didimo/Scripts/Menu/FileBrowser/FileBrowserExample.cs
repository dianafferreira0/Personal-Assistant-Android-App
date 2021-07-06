using UnityEngine;

namespace Didimo.Menu.FileBrowser
{
    /// <summary>
    /// Simple example on how to use the <see cref="FileBrowser"/> class.
    /// </summary>
    public class FileBrowserExample : MonoBehaviour
    {
        [SerializeField]
        ImageFileBrowser fileBrowser = null;

        // Use this for initialization
        void Start()
        {
            fileBrowser.OnOpenAction = path =>
            {
                Debug.Log("Seleccted file: " + path);
            };
        }
    }
}