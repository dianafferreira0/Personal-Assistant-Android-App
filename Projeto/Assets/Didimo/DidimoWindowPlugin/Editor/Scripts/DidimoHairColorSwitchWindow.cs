using UnityEditor;
using UnityEngine;
using System.IO;
using Didimo.Networking.DataObjects;
using System.Collections.Generic;
using System;
using Didimo.DidimoManagement;
using Didimo.Rendering;
using Didimo.Utils.Serialization.SimpleJSON;

namespace Didimo.Editor.Window
{
    [ExecuteInEditMode]
    public class DidimoHairColorSwitchWindow : EditorWindow
    {
        static string hairMatDirPath = "Assets/Didimo/Hair/Template_2.0/Materials"; /// path to hair materials
        static string skinMatName = "skinMat"; /// skin material name for changing hair cap color

        static string[] hairPrefabs = { //prefabs for changing hairstyle - it will be read from the selected didimo's folder: .../HairPrefabs/Hairstyle_001_...
            "",
            "Hairstyle_001",
            "Hairstyle_006",
            "Hairstyle_007",
            "Hairstyle_008",
            "Hairstyle_009",
            "Hairstyle_010",
            "Hairstyle_011",
            "Hairstyle_012",
            "Hairstyle_013",
            "Hairstyle_014"
        };

        [SerializeField]
        GameObject didimoToUpdate = null; /// reference to the didimo game object for changing hair cap color

        [SerializeField]
        DefaultAsset didimoAssetsFolder = null; /// reference to the didimo assets folder (we need this to instantiate the correct prefabs)

        private int selectedHairstyle = -1;
        private int selected = -1; //color

        private GameObject currentHair = null;

        #region UI

        [MenuItem("Window/Didimo/Change Hairstyle")]
        public static void ShowHairColorSwitchWindow()
        {
            DidimoHairColorSwitchWindow window = (DidimoHairColorSwitchWindow)EditorWindow.GetWindow(typeof(DidimoHairColorSwitchWindow));
            window.minSize = new Vector2(300, 250);
            window.titleContent = new GUIContent("Change Hairstyle");
        }

        private void OnGUI()
        {
            GUILayout.TextArea("Changes the hairstyle of a didimo on the scene. \n\n" +
                "Pick a didimo game object on the scene, add the reference his assets folder, and you will be able to switch hairstyle and color. \n");

            GUILayout.BeginHorizontal();

            GameObject previousDidimoToUpdate = didimoToUpdate;
            didimoToUpdate = (GameObject)EditorGUILayout.ObjectField("Didimo To Update", didimoToUpdate, typeof(GameObject), true);

            if(didimoToUpdate != previousDidimoToUpdate)
                selected = -1;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();


            DefaultAsset previousDidimoAssetsFolder = null;
            didimoAssetsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Didimo Assets Folder", didimoAssetsFolder, typeof(DefaultAsset), true);
            if (didimoAssetsFolder != null && didimoAssetsFolder != previousDidimoAssetsFolder)
            {
                //must contain HairPrefabs sub-folder
                string didimoAssetsFolderPath = AssetDatabase.GetAssetPath(didimoAssetsFolder);
                if ( Directory.Exists(didimoAssetsFolderPath+ "/HairPrefabs") )
                {
                }
                else {
                    EditorGUILayout.HelpBox(
                    "Invalid folder! It should contain the didimo's hairstyle prefabs.",
                    MessageType.Error,
                    true);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            int previousHairstyle = selectedHairstyle;
            string[] hairstyle_options = new string[]
            {
                "Bald", "Hairstyle 1", "Hairstyle 6", "Hairstyle 7", "Hairstyle 8", "Hairstyle 9", "Hairstyle 10", "Hairstyle 11", "Hairstyle 12", "Hairstyle 13", "Hairstyle 14"
            };
            selectedHairstyle = EditorGUILayout.Popup("Hairstyle", selectedHairstyle, hairstyle_options);

            if (selectedHairstyle != previousHairstyle)
            {
                switchHairStyle(selectedHairstyle);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            int previousSelected = selected;
            string[] options = new string[]
            {
                "Auburn", "Black", "Chestnut Brown", "Copper", "Dark Brown", "Gray", "Light Blonde", "Light Brown", "Medium Blonde", "White"
            };
            selected = EditorGUILayout.Popup("Color", selected, options);

            if (selected != previousSelected)
            {
                switch (selected)
                {
                    case 0: switchHairColor("auburn"); break;
                    case 1: switchHairColor("black"); break;
                    case 2: switchHairColor("chestnutBrown"); break;
                    case 3: switchHairColor("copper"); break;
                    case 4: switchHairColor("darkBrown"); break;
                    case 5: switchHairColor("gray"); break;
                    case 6: switchHairColor("lightBlonde"); break;
                    case 7: switchHairColor("lightBrown"); break;
                    case 8: switchHairColor("mediumBlonde"); break;
                    case 9: switchHairColor("white"); break;
                    default: break;
                }

            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region Logic
        void switchHairStyle(int selectedHairstyle)
        {
            //Log.info("switching to {0}", selectedHairstyle);

            string prefab_prefix = hairPrefabs[selectedHairstyle].ToLower();

            string didimoAssetsFolderPath = AssetDatabase.GetAssetPath(didimoAssetsFolder);
            string searchFolder = didimoAssetsFolderPath + "/HairPrefabs";

            /// find the prefabs files
            string[] files = AssetDatabase.FindAssets("", new[] { searchFolder });
            if (files.Length == 0)
            {
                Log.error("failed to find hair prefabs in {0}", searchFolder);
                return;
            }

            CleanUpPreviousHair();

            if (prefab_prefix.CompareTo("") != 0)
            {
                foreach (string s in files)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(s);
                    string prefabName = prefabPath.Replace(searchFolder + "/", "").ToLower();

                    if (prefabName.StartsWith(prefab_prefix))
                    {
                        /// load the prefab
                        Log.info("load {0}", prefabPath);
                        GameObject hairPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
                        //instantiate prefab
                        ApplyHair(selectedHairstyle, hairPrefab);

                        if(selected!=-1)
                            switchHairColor(selected);
                        break;
                    }
                }
            }

            Camera.main.Render();
        }

        private void ApplyHair(int hairstyleIndex, GameObject prefab)
        {
            if (prefab != null)
            {
                Transform headJointTransform = FindRecursive(didimoToUpdate.transform, "jnt_m_headEnd_001");

                if (headJointTransform != null)
                {
                    Transform t = didimoToUpdate.transform;
                    //apply hair
                    Transform currentHairTransform = Instantiate(prefab.transform, new Vector3(0f, 0f, 0f), Quaternion.identity);
                    currentHairTransform.parent = t;
                    currentHairTransform.localPosition = new Vector3(0f, 0f, 0f);
                    currentHairTransform.localRotation = Quaternion.identity;
                    currentHairTransform.localScale = new Vector3(1f, 1f, 1f);
                    currentHair = currentHairTransform.gameObject;
                    currentHairTransform.parent = headJointTransform;
                }
            }
            else Debug.Log("hair prefab is null");
        }

        /// <summary>
        /// Find for a game object with the provided name, starting from the given transform.
        /// </summary>
        /// <param name="transform">The extended Tranform object.</param>
        /// <param name="name">The name to look for.</param>
        /// <returns>A Transform object with the given name, null if not found.</returns>
        public static Transform FindRecursive(Transform transform, string name)
        {
            if (transform.name.Equals(name))
            {
                return transform;
            }

            Transform result = transform.Find(name);
            if (result != null)
                return result;
            foreach (Transform child in transform)
            {
                result = FindRecursive(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void CleanUpPreviousHair()
        {
            if (currentHair != null)
            {
                DestroyImmediate(currentHair);

                //clear haircap
                /// find the current skin material on the scene (for setting the hair cap)
                Material skinMaterial = findMaterial(skinMatName);
                if (!skinMaterial)
                {
                    Log.error("couldn't find skin material {0}", skinMatName);
                    return;
                }

                skinMaterial.SetTexture("hairCapSampler", null);
            }
            currentHair = null;
        }

        void switchHairColor(int index)
        {
            switch (index)
            {
                case 0: switchHairColor("auburn"); break;
                case 1: switchHairColor("black"); break;
                case 2: switchHairColor("chestnutBrown"); break;
                case 3: switchHairColor("copper"); break;
                case 4: switchHairColor("darkBrown"); break;
                case 5: switchHairColor("gray"); break;
                case 6: switchHairColor("lightBlonde"); break;
                case 7: switchHairColor("lightBrown"); break;
                case 8: switchHairColor("mediumBlonde"); break;
                case 9: switchHairColor("white"); break;
                default: break;
            }
        }

        void switchHairColor(string hairColorName)
        {
            //Log.info("switching to {0}", hairColorName);

            /// find the base hair material
            Material hairMaterial = findMaterial("Mat_SDK_Hairstyle_");
            if (!hairMaterial)
            {
                Log.error("couldn't find hair material");
                return;
            }

            /// find the current skin material on the scene (for setting the hair cap)
            Material skinMaterial = findMaterial(skinMatName);
            if (!skinMaterial)
            {
                Log.error("couldn't find skin material {0}", skinMatName);
                return;
            }

            ///set hair cap depending on hair style
            string index_to_parse = hairMaterial.name.Replace("Mat_SDK_Hairstyle_", "").Replace("(Clone)", "").Trim();
            Texture2D haircapTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Didimo/Hair/Template_2.0/Textures/hairCap" + index_to_parse + ".png", typeof(UnityEngine.Object));
            skinMaterial.SetTexture("hairCapSampler", haircapTexture);

            /// find the material state file
            string matStateDirPath = hairMatDirPath + "/HairColors";
            string[] files = AssetDatabase.FindAssets(hairColorName, new[] { matStateDirPath });
            if (files.Length == 0)
            {
                Log.error("failed to find material state file in {0}", matStateDirPath);
                return;
            }

            /// load the material state file
            string matStatePath = AssetDatabase.GUIDToAssetPath(files[0]);
            TextAsset matStateJson = (TextAsset)AssetDatabase.LoadAssetAtPath(matStatePath, typeof(TextAsset));
            var jsonData = JSON.Parse(matStateJson.text);

            // NOTE: assuming the first material is the one (the Maya side script ensures only one material is exported)
            JSONNode hairMat = jsonData["materials"][0];
            if (!hairMat["shader"].Equals("hair"))
            {
                Log.error("first material wasn't a hair material");
                return;
            }
            foreach (JSONNode p in hairMat["parameters"])
            {
                string pName = p["name"];
                var pValue = p["value"];
                string pType = p["type"];

                switch (pType)
                {
                    case "enum":
                        {
                            // Log.info("enum name: {0}, value {1}", pName, pValue);
                            hairMaterial.SetInt(pName, pValue);
                            break;
                        }

                    case "bool":
                        {
                            // Log.info("bool name: {0}, value {1}", pName, pValue);
                            hairMaterial.SetInt(pName, pValue);
                            break;
                        }

                    case "float":
                        {
                            // Log.info("float name: {0}, value {1}", pName, pValue);
                            hairMaterial.SetFloat(pName, pValue);
                            break;
                        }
                    case "float3":
                        {
                            // Log.info("float3 name: {0}, value {1}", pName, pValue);
                            hairMaterial.SetVector(pName, new Vector3(pValue[0], pValue[1], pValue[2]));

                            if (pName.Equals("diffColor")) /// set the hair cap color
                            {
                                Vector3 hairCapColor = new Vector3(pValue[0], pValue[1], pValue[2]);
                                //hairCapColor = hairCapColor;// * 0.4f; /// doing it anyways
                                Log.info("setting hair cap color to {0}", hairCapColor);
                                skinMaterial.SetVector("hairColor", hairCapColor);
                            }
                            break;
                        }
                    case "float4":
                        {
                            // Log.info("float4 name: {0}, value {1}", pName, pValue);
                            hairMaterial.SetVector(pName, new Vector4(pValue[0], pValue[1], pValue[2], pValue[3]));
                            break;
                        }
                }
            }

            /// set hair cap color
            JSONNode skinMat = jsonData["materials"][1];
            if (!skinMat["shader"].Equals("skin"))
            {
                Log.error("second material wasn't a skin material");
                return;
            }
            JSONNode sp = skinMat["parameters"][0]; /// NOTE: assume there's only the hair cap color parameter
            if (!sp["name"].Equals("hairColor"))
            {
                Log.error("skin material parameter wasn't hairColor");
                return;
            }
            var v = sp["value"];
            skinMaterial.SetVector("hairColor", new Vector3(v[0], v[1], v[2]));

            Log.info("all done");
        }

        Material findMaterial(string matName)
        {
            Renderer[] list = didimoToUpdate.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in list)
            {
                if (r.sharedMaterial.name.StartsWith(matName))
                    return r.sharedMaterial;
            }

            Log.error("couldn't find material {0}", matName);
            return null;
        }

        #endregion

    }
}



