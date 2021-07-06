using UnityEngine;

namespace Didimo.Menu.Utils
{
    public class ShowNavigationBar : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {
            // Makes navigation bar visible and enables turning on the status bar (requires more code: unityShowAndroidStatusBar.aar)
            Screen.fullScreen = false;
        }

    }
}