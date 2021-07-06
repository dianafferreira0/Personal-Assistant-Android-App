using Didimo.Editor.Window;
using Didimo.Utils.ZipUtils;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Utils
{
    class DidimoModelImporter : AssetPostprocessor
    {
        public static bool isImportingDidimos = false;

        void OnPreprocessModel()
        {
            if (isImportingDidimos)
            {
                ModelImporter modelImporter = (ModelImporter)assetImporter;
#if UNITY_2017_3_OR_NEWER
                modelImporter.materialLocation = ModelImporterMaterialLocation.External;
#endif
                modelImporter.materialSearch = ModelImporterMaterialSearch.Local;
            }
        }

        void OnPreprocessTexture()
        {
            if (isImportingDidimos && assetPath.ToLower().Contains("normal"))
            {
                TextureImporter textureImporter = (TextureImporter)assetImporter;
                textureImporter.convertToNormalmap = true;
                textureImporter.textureType = TextureImporterType.NormalMap;
            }
        }
    }

    /// <summary>
    /// Manager for the files that represent a Didimo, i.e. skin texture, normal map, fbx and json.
    /// </summary>
    public class DidimoFilesManager
    {
        delegate void ProgressDelegate(string progressMessage);

        /// <summary>
        /// Create a path to the fbx file, for the target folder, starting at "/Assets".
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The local path to the fbx file.</returns>
        public static string FbxLocalPath(string folderFullPath)
        {
            return TransformToLocalPath(FbxPath(folderFullPath));
        }

        /// <summary>
        /// Create a path to the fbx file, for the target folder.
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The full path to the fbx file.</returns>
        public static string FbxPath(string folderFullPath)
        {
            return Path.Combine(folderFullPath, "avatar_model.fbx");
        }

        /// <summary>
        /// Create a path to the skin texture file, for the target folder, starting at "/Assets".
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The local path to the skin texture file.</returns>
        public static string SkinLocalPath(string folderFullPath)
        {
            return TransformToLocalPath(SkinPath(folderFullPath));
        }

        /// <summary>
        /// Create a path to the skin texture file, for the target folder.
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The full path to the skin texture file.</returns>
        public static string SkinPath(string folderFullPath)
        {
            return Path.Combine(folderFullPath, "model_retopology.jpg");
        }

        /// <summary>
        /// Create a path to the normal map texture file, for the target folder, starting at "/Assets".
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The local path to the normal map texture file.</returns>
        public static string NormalMapLocalPath(string folderFullPath)
        {
            return TransformToLocalPath(NormalMapPath(folderFullPath));
        }

        /// <summary>
        /// Create a path to the normal map texture file, for the target folder.
        /// </summary>
        /// <param name="folderFullPath">The full path to the folder where the didimo files will be stored.</param>
        /// <returns>The full path to the normal map texture file.</returns>
        public static string NormalMapPath(string folderFullPath)
        {
            return Path.Combine(folderFullPath, "model_retopology_normalmap.png");
        }

        /// <summary>
        /// Save the didimo files to a folder. Will automatically import the files to the project.
        /// </summary>
        /// <param name="path">The full path to the folder where to save the Didimo files.</param>
        /// <param name="fbx">The fbx file.</param>
        /// <param name="skin">The skin texture file.</param>
        /// <param name="normalMap">The normal map texture file.</param>
        ///<returns>An exception if an exception was raised, null otherwise.</returns>
        public static System.Exception SaveToFolder(string path, byte[] fbx, Texture2D skin, Texture2D normalMap)
        {
            try
            {
                //Save the files into the project
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path + "/Model");
                File.WriteAllBytes(FbxPath(path + "/Model"), fbx);
                File.WriteAllBytes(SkinPath(path + "/Model"), skin.EncodeToJPG());
                File.WriteAllBytes(NormalMapPath(path + "/Model"), normalMap.EncodeToPNG());

                //Import the assets into the project
                AssetDatabase.ImportAsset(SkinLocalPath(path + "/Model"));
                AssetDatabase.ImportAsset(NormalMapLocalPath(path + "/Model"));
                TextureImporter ti = AssetImporter.GetAtPath(NormalMapLocalPath(path + "/Model")) as TextureImporter;
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
                AssetDatabase.ImportAsset(FbxLocalPath(path + "/Model"));
                ModelImporter mi = AssetImporter.GetAtPath(FbxLocalPath(path + "/Model")) as ModelImporter;
                mi.materialSearch = ModelImporterMaterialSearch.Local;
                mi.SaveAndReimport();
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save the Didimo files. " + e.Message);
                return e;
            }
        }

        /// <summary>
        /// Unzip and save the didimo files to a folder. Will automatically import the files to the project.
        /// </summary>
        /// <param name="path">The full path to the folder where to save the Didimo files.</param>
        /// <param name="zip">The Didimo zip file.</param>
        ///<returns>An exception if an exception was raised, null otherwise.</returns>
        public static System.Exception SaveToFolder(string path, byte[] zip)
        {
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "/Model");
            List<string> importedFiles = new List<string>();

            // Stop Unity from Auto-importing the assets
            AssetDatabase.StartAssetEditing();

            DidimoModelImporter.isImportingDidimos = true;

            try
            {
                ZipUtil.DecompressToDirectory(zip, path + "/Model", (fileName, progress) =>
                    {
                        importedFiles.Add(fileName);
                        EditorUtility.DisplayProgressBar("Decompressing files", "Decompressing " + fileName + ", please wait. ", progress);
                        //Debug.Log("Decompressing " + fileName + ", please wait. ");
                    });

                EditorUtility.ClearProgressBar();
            }
            catch (System.Exception e)
            {

                Debug.LogError("Failed to save the Didimo files. " + e.Message);

                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                DidimoModelImporter.isImportingDidimos = false;


                return e;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            DidimoModelImporter.isImportingDidimos = false;

            //BUGFIX - UPFATE EYELASHES SHADER
            string relativeFolderPath = path.Replace(Application.dataPath, "Assets");
            Material eyeLashesMat = AssetDatabase.LoadAssetAtPath(relativeFolderPath + "/Model" + "/Materials/coloured_lashes.mat", (typeof(Material))) as Material;
            if (eyeLashesMat != null)
            {
                eyeLashesMat.SetFloat("_Mode", 1);
                eyeLashesMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                eyeLashesMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                eyeLashesMat.SetInt("_ZWrite", 0);
                eyeLashesMat.DisableKeyword("_ALPHATEST_ON");
                eyeLashesMat.EnableKeyword("_ALPHABLEND_ON");
                eyeLashesMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                eyeLashesMat.renderQueue = 3000;
            }
            else Debug.LogWarning("COULD NOT FIX THE EYELASHES SHADER - Please change manually the material's Rendering Mode to \"Cutout\" ");

            return null;
        }


        /// <summary>
        /// Transform a path into a local path starting at the project's "Assets" folder.
        /// </summary>
        /// <param name="fullPath">The full path to convert to local path.</param>
        /// <returns>The full path converted to local path.</returns>
        private static string TransformToLocalPath(string fullPath)
        {
            return "Assets" + fullPath.Replace(Application.dataPath, string.Empty);
        }
    }
}