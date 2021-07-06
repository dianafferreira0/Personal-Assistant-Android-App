using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Window
{
    public class DidimoOpenHelpMenuOption
    {
        [MenuItem("Window/Didimo/Open documentation")]
        public static void OpenDocumentationPage()
        {
            Application.OpenURL("https://docs.didimo.co");
        }
    }
}