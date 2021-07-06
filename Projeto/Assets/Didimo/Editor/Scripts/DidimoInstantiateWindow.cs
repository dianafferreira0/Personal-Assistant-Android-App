using Didimo.DidimoManagement;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace Didimo.Editor
{
    public class DidimoInstantiateWindow : EditorWindow
    {
        string modelPath;
        string didimoName = "didimo";

        [SerializeField]
        GameObject didimoToUpdate = null;
        [SerializeField]
        bool updateOnly = false;

        [MenuItem("Window/Didimo/Instantiate didimo")]
        public static void ShowInstantiateWindow()
        {
            DidimoInstantiateWindow window = (DidimoInstantiateWindow)EditorWindow.GetWindow(typeof(DidimoInstantiateWindow));
            window.minSize = new Vector2(300, 250);
            window.titleContent = new GUIContent("Instantiate didimo");
        }

        private void UpdateModel()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Updating Didimo", "Deserializing...", 0);

                string modelFolder = Path.GetDirectoryName(modelPath);

                try
                {
                    StreamReader reader = new StreamReader(modelPath);
                    IDidimoModel model = DidimoModelFactory.CreateDidimoModel(reader.ReadToEnd());
                    reader.Close();
                    EditorUtility.DisplayProgressBar("Updating Didimo", "Importing model...", 1);
                    Transform instantiateInTransform;
                    if (updateOnly)
                    {
                        instantiateInTransform = didimoToUpdate.transform;
                    }
                    else
                    {
                        instantiateInTransform = new GameObject(didimoName).transform;
                    }
                    model.InstantiateDidimo(instantiateInTransform, modelFolder, updateOnly);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void OnGUI()
        {
            GUILayout.TextArea("Instantiate a didimo from your project assets. Supports textures, materials, geometry, skeleton, constraints, and the Didimo animation system." +
                " Select a didimo json file and the didimo will be added to the active scene.");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Didimo Name", GUILayout.ExpandWidth(false));
            didimoName = GUILayout.TextField(didimoName, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.TextArea("Instantiate the didimo in the scene. No files in the project will be created. If Update Only is specified, we will be updating an existing didimo instead.");
            if (updateOnly)
            {
                didimoToUpdate = (GameObject)EditorGUILayout.ObjectField("Didimo To Update", didimoToUpdate, typeof(GameObject), true);
            }
            GUILayout.BeginHorizontal();
            updateOnly = GUILayout.Toggle(updateOnly, "Update Only", GUILayout.Width(100));
            if (GUILayout.Button("Instantiate"))
            {
                if (updateOnly && didimoToUpdate == null)
                {
                    Debug.LogError("Please select a didimo to update.");
                    return;
                }

                modelPath = EditorUtility.OpenFilePanel("Select Didimo Model", "", "js,json");
                if (!string.IsNullOrEmpty(modelPath))
                {
                    UpdateModel();
                }
            }
            GUILayout.EndHorizontal();

        }
    }
}