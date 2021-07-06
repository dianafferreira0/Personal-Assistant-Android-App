using Didimo.Editor.Utils.Coroutines;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Didimo.Editor.Window
{
    public class InstallDidimoWindow : EditorWindow
    {
        /// <summary>
        /// Show the Didimo Install Window
        /// </summary>
        [MenuItem("Window/Didimo/Didimo Install Window")]
        public static void ShowWindow()
        {
            InstallDidimoWindow window = (InstallDidimoWindow)GetWindow(typeof(InstallDidimoWindow));
            window.minSize = new Vector2(150, 150);
            window.titleContent = new GUIContent("Didimo Install");
        }

        void OnGUI()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("To import the Didimo Plugin, please press the 'Install' button. The package will be downloaded and automatically imported into your project. \n\nOnce finished, you can open the didimo window by going to Window -> Didimo -> Didimo  Plugin Window.", GUI.skin.textArea);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Install"))
            {
                EditorUtility.DisplayProgressBar("Didimo Install", "Installing the Didimo Plugin...", 0);
                Install(() =>
                {
                    EditorUtility.ClearProgressBar();

                }, exception =>
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Error", "An error occurred while installing the Didimo Editor Package. Please try again later. Error message: " + exception.Message, "OK");
                });
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        public void Install(Action onDone, Action<Exception> onError)
        {
            EditorCoroutineManager coroutineManager = new EditorCoroutineManager();
            coroutineManager.StartCoroutine(InstallAsync(onDone, onError));
        }

        private IEnumerator InstallAsync(Action onDone, Action<Exception> onError)
        {

            string packagePath = Application.temporaryCachePath + System.IO.Path.PathSeparator + "didimo_editor.unitypackage";
            string packageUrl = "https://static.didimo.co/editor/didimo-editor-latest.unitypackage";

            UnityWebRequest packageRequest = UnityWebRequest.Get(packageUrl);

#if UNITY_2017_3_OR_NEWER
            packageRequest.SendWebRequest();
#else
            packageRequest.Send();
#endif

            while (!packageRequest.isDone || packageRequest.downloadProgress < 1.0f)
            {
                yield return new EditorWaitForSeconds(1f);
                EditorUtility.DisplayProgressBar("Didimo Installer", "Installing the Didimo Plugin...", packageRequest.downloadProgress);
            }

            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(packageRequest.error))
            {
                onError(new Exception(packageRequest.error));
                yield break;
            }

            try
            {
                System.IO.File.WriteAllBytes(packagePath, packageRequest.downloadHandler.data);
                AssetDatabase.ImportPackage(packagePath, false);
                System.IO.File.Delete(packagePath);
            }
            catch (Exception e)
            {
                if (System.IO.File.Exists(packagePath))
                {
                    System.IO.File.Delete(packagePath);
                }
                onError(e);

                yield break;
            }

            onDone();
        }
    }
}
