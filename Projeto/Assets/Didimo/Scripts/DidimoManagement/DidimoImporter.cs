using Didimo.Menu;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityZip;

namespace Didimo.DidimoManagement
{
    public partial class DidimoImporter : DidimoImporterBase
    { }

    public class DidimoImporterBase : MonoBehaviour
    {
        public GameObject ParentPlaceholder;

        public string didimoKey = "";

        protected bool isLoadingDidimo = false;

        public class ImportStatistics
        {
            public float downloadTime;
            public float importTime;
            public bool downloadCached;
        }

        public IEnumerator ImportDidimo(CoroutineManager coroutineManager,
            string didimoKey,
            List<DidimoMetadataDataObject> metadata,
            Action<GameObject> successDelegate,
            Action<Exception> errorDelegate,
            DownloadHandler downloadHandler = null)
        {
            yield return ImportDidimo(coroutineManager,
                didimoKey,
                metadata,
                (gameObject, ImportStatistics) =>
                {
                    successDelegate(gameObject);
                },
                errorDelegate,
                downloadHandler
                );
        }

        public IEnumerator ImportDidimo(CoroutineManager coroutineManager,
        string didimoKey,
        List<DidimoMetadataDataObject> metadata,
        Action<GameObject, ImportStatistics> successDelegate,
        Action<Exception> errorDelegate,
        DownloadHandler downloadHandler = null)
        {

            ImportStatistics importStatistics = new ImportStatistics();

            importStatistics.downloadTime = Time.realtimeSinceStartup;
            importStatistics.importTime = 0;
            importStatistics.downloadCached = false;

            //delete current didimo if it exists
            for (int i = 0; i < ParentPlaceholder.transform.childCount; i++)
            {
                if (!ParentPlaceholder.transform.GetChild(i).name.Equals("RotationPivot"))
                    Destroy(ParentPlaceholder.transform.GetChild(i).gameObject);
            }

            isLoadingDidimo = true;
            this.didimoKey = didimoKey;

            string importPath = "";

            yield return ServicesRequests.GameInstance.FetchDidimoZip(coroutineManager,
                 "unity",
                 didimoKey,
                 (zippath, zipBytes) =>
                 {

                     importStatistics.downloadTime = Time.realtimeSinceStartup - importStatistics.downloadTime;
                     importStatistics.importTime = Time.realtimeSinceStartup;

                     bool canSkipUnzipStep = false;

                     string basefolderpath = WWWCachedRequest.GetCachePath("", null);

                     DirectoryInfo currDidimoDirInfo = new DirectoryInfo(basefolderpath + didimoKey + "/");
                     if (!currDidimoDirInfo.Exists)
                     {
                         canSkipUnzipStep = false;
                         //Debug.Log("ASSETS FOLDER FOR SELECTED DIDIMO - NOT FOUND ... CREATING ... ");
                         Directory.CreateDirectory(basefolderpath + didimoKey + "/");
                         currDidimoDirInfo = new DirectoryInfo(basefolderpath + didimoKey + "/");
                     }
                     else if (currDidimoDirInfo.Exists && currDidimoDirInfo.GetFiles().Length > 0)
                     {
                         canSkipUnzipStep = true;
                     }

                     importStatistics.downloadCached = canSkipUnzipStep;

                     importPath = currDidimoDirInfo.FullName;

                     if (!canSkipUnzipStep)
                     {
                         Exception zipExtractionException = ExtractZipToFolder(importPath, didimoKey, zippath);
                         if (zipExtractionException != null)
                         {
                             Debug.LogError("ImportDidimo - Extraction failed - " + zipExtractionException.Message);
                             isLoadingDidimo = false;
                             errorDelegate(zipExtractionException);
                         }
                     }
                 },
                 exception =>
                 {
                     Debug.LogError("ImportDidimo - download failed - " + exception.Message);
                     isLoadingDidimo = false;
                     //Time.timeScale = oldTimeScale;
                     errorDelegate(exception);
                 },
                 downloadHandler);

            if (!isLoadingDidimo)
                yield break;

            LoadingOverlay.Instance.ShowLoadingMenu(() => { }, "Importing model");

            string modelName = "avatar_model.json";
            //load json file from
            string modelPath = importPath + modelName;

            //Debug.Log("ImportDidimo - Model path: " + modelPath);
            GameObject didimoGameObject = ImportModel(importPath, modelPath);
            importStatistics.importTime = Time.realtimeSinceStartup - importStatistics.importTime;

            successDelegate(didimoGameObject, importStatistics);
        }


        /// <summary>
        /// Unzip and save the didimo files to a folder.
        /// </summary>
        /// <param name="path">The full path to the folder where to save the Didimo files.</param>
        /// <param name="zip">The Didimo zip file.</param>
        ///<returns>An exception if an exception was raised, null otherwise.</returns>
        public static System.Exception ExtractZipToFolder(string basePath, string didimoKey, string zippath)
        {
            string didimoFolderPath = basePath;// + didimoKey;
                                               //Debug.Log("ExtractZipToFolder - Creating folder: " + didimoFolderPath);
                                               //Directory.CreateDirectory(didimoFolderPath);
            try
            {
                ZipUtil.Unzip(zippath, didimoFolderPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save the Didimo files. " + e.Message + " StackTrace: \n" + e.StackTrace);
                return e;
            }

            return null;
        }

        protected GameObject ImportModel(string importPath, string modelPath)
        {
            try
            {
                Debug.Log("Importing Didimo - Deserializing... " + modelPath);
                StreamReader reader = new StreamReader(modelPath);
                //DidimoModelDataObject result = DataObject.LoadFromJson<DidimoModelDataObject>(reader.ReadToEnd());
                IDidimoModel model = DidimoModelFactory.CreateDidimoModel(reader.ReadToEnd());
                reader.Close();

                Transform didimoModelParent = ParentPlaceholder.transform;
                model.InstantiateDidimo(didimoModelParent, importPath, false);

                return didimoModelParent.gameObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read the Didimo model from path. " + e.Message + " StackTrace: \n" + e.StackTrace);
                return null;
            }
        }
    }
}
