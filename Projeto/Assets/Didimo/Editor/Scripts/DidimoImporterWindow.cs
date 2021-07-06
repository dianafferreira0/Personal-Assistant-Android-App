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
    public class DidimoImporterWindow : EditorWindow
    {
        string modelPath;
        string didimoName = "didimo";

        [SerializeField]
        DefaultAsset importIntoFolder = null;
        [SerializeField]
        GameObject didimoToUpdate = null;
        [SerializeField]
        bool updateOnly = false;

        [MenuItem("Window/Didimo/Import didimo")]
        public static void ShowImportWindow()
        {
            DidimoImporterWindow window = (DidimoImporterWindow)EditorWindow.GetWindow(typeof(DidimoImporterWindow));
            window.minSize = new Vector2(300, 250);
            window.titleContent = new GUIContent("Import didimo");
        }

        private void SaveAndGetTextureAsset(string inputFolder, string textureName, string textureType, ref Texture2D texture)
        {
            string textureOriginalPath = Path.Combine(inputFolder, textureName);
            string textureDestinationPath = Path.Combine(AssetDatabase.GetAssetPath(importIntoFolder), textureName);
            if (File.Exists(textureOriginalPath))
            {
                File.Copy(textureOriginalPath, textureDestinationPath, true);
                AssetDatabase.ImportAsset(textureDestinationPath);
                TextureImporter textureImporter = AssetImporter.GetAtPath(textureDestinationPath) as TextureImporter;
                TextureImporterSettings settings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(settings);

                if (textureType.Equals("normal"))
                {
                    settings.textureType = TextureImporterType.NormalMap;
                }
                else
                    if (textureType.Equals("diffuse"))
                {
                    settings.textureType = TextureImporterType.Default;

                }
                else
                {
                    Debug.LogError("Texture type not supported ");
                }
                textureImporter.SetTextureSettings(settings);

                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.assetPath);
            }
            else
            {
                Debug.LogWarning("Failed to load texture '" + textureOriginalPath + "' for mesh '" + textureName);
            }
        }

        private void ImportModel()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Importing Didimo", "Deserializing...", 0);
                string modelFolder = Path.GetDirectoryName(modelPath);

                GameObject didimo = new GameObject();
                didimo.name = didimoName;

                try
                {
                    StreamReader reader = new StreamReader(modelPath);
                    IDidimoModel model = DidimoModelFactory.CreateDidimoModel(reader.ReadToEnd());
                    reader.Close();
                    EditorUtility.DisplayProgressBar("Importing Didimo", "Importing model...", 1);
                    model.ImportDidimo(didimoName, didimo.transform, modelFolder, AssetDatabase.GetAssetPath(importIntoFolder));
                }
                catch (System.Exception e)
                {
                    DestroyImmediate(didimo);
                    Debug.LogError(e);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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
            GUILayout.TextArea("Import a didimo outputed by the Didimo pipeline. Supports textures, materials, geometry, skeleton, constraints, and the Didimo animation system." +
                " Select a didimo json file and the file will be imported into the scene.");

            importIntoFolder = (DefaultAsset)EditorGUILayout.ObjectField("Import Into Folder", importIntoFolder, typeof(DefaultAsset), false);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Didimo Name", GUILayout.ExpandWidth(false));
            didimoName = GUILayout.TextField(didimoName, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.TextArea("Import into the project, and add didimo prefab to the scene. Creates the didimo files (like meshes and textures) in the 'import into folder' directory. The didimo prefab will have the name specified in 'Didimo Name'");
            if (GUILayout.Button("Import"))
            {
                if (importIntoFolder == null)
                {
                    Debug.LogError("Please select a destination Import Folder.");
                    return;
                }

                if (string.IsNullOrEmpty(didimoName))
                {
                    Debug.LogError("Please select a name for the didimo.");
                    return;
                }

                modelPath = EditorUtility.OpenFilePanel("Select Didimo Model", "", "js,json");
                if (!string.IsNullOrEmpty(modelPath))
                {
                    ImportModel();
                }
            }

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