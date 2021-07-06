using Didimo.DidimoManagement;
using Didimo.Editor.Utils;
using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Networking.Header;
using Didimo.Utils.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Window
{
    /// <summary>
    /// The Didimo Window allows you to manage your didimos.
    /// It enables you to create Didimos and download them into the current project.
    /// </summary>
    public class DidimoWindow : EditorWindow
    {
        public enum APIConnectionState
        {
            NotSetup,
            Disconnected,
            Connecting,
            Connected
        }

        const int headerHeight = 20;
        const int loadMoreHeight = 30;
        const int geariconFontSize = 14;
        const int scrollViewWidth = 60;
        const string loginStateKey = "didimo.loginstate";

        private bool isDeformationSupported = false;

        APIConnectionState ConnectionState
        {
            get { return (APIConnectionState)EditorPrefs.GetInt(loginStateKey, (int)APIConnectionState.NotSetup); }
            set { EditorPrefs.SetInt(loginStateKey, (int)value); }
        }

        [SerializeField]
        Font iconFont;
        [SerializeField]
        DidimoWindowThumbnailsView didimoThumbnailScrollview;
        [SerializeField]
        DidimoWindowPreviewView didimoPreviewView;
        [SerializeField]
        DidimoWindowSetupAuthKeyView didimoSetupAuthKeyView;
        [SerializeField]
        UserProfileDataObject userProfile;
        CoroutineManager coroutineManager;

        /// <summary>
        /// Paginated list for loading and displaying didimos
        /// </summary>
        DidimoGalleryCollectionFetcher didimoCollectionFetcher;
        /// <summary>
        /// When the is nextPageThreshold items to the end of the list, the next page will be requested
        /// </summary>
        public int nextPageThreshold = 10;
        public Sprite defaultDidimoSprite;
        protected List<DidimoDataObject> didimos;
        protected bool waitingForNextPage = false;
        public class DidimoDataObject
        {
            public DidimoDataObject(System.Action<DidimoDataObject> updateDelegate, CoroutineManager coroutineManager, Sprite sprite, string key, string templateVersion, List<DidimoMetadataDataObject> metadata, bool isDone, bool failed)
            {
                if (coroutineManager == null)
                    coroutineManager = new EditorCoroutineManager();

                this.sprite = sprite;
                this.key = key;
                this.templateVersion = templateVersion;
                this.metadata = metadata;
                progressPercentage = 0;
                this.isDone = isDone;
                this.failed = failed;

                if (!failed)
                {
                    if (isDone)
                    {
                        FetchPreview(updateDelegate, coroutineManager);
                    }
                    else
                    {
                        // No need, the update will return updated didimo progress
                        //coroutineManager.StartCoroutine(UpdateProgress(updateDelegate, coroutineManager));
                    }
                }
            }

            static float progressUpdateInterval = 5;
            public Sprite sprite;
            public string key;
            public string templateVersion;
            public List<DidimoMetadataDataObject> metadata;
            public int progressPercentage;
            public bool failed = false;
            public bool isDone;

            void FetchPreview(System.Action<DidimoDataObject> updateDelegate, CoroutineManager coroutineManager)
            {
                if (coroutineManager == null)
                    coroutineManager = new EditorCoroutineManager();

                ServicesRequests.GameInstance.DownloadDidimoPreview(
                            coroutineManager,
                            PreviewType.FrontPerspective,
                            key,
                            texture =>
                            {
                                Rect rect = new Rect(0, 0, texture.width, texture.height);
                                sprite = Sprite.Create(texture, rect, sprite.pivot);
                                updateDelegate(this);
                                isDone = true;
                            },
                            exception =>
                            {
                                isDone = true;
                                failed = true;
                                Debug.LogError("Error fetching preview: " + exception.Message);
                            });
            }

            IEnumerator UpdateProgress(System.Action<DidimoDataObject> updateDelegate, CoroutineManager coroutineManager)
            {

                while (progressPercentage != 100)
                {
                    if (coroutineManager == null)
                        coroutineManager = new EditorCoroutineManager();

                    ServicesRequests.GameInstance.DidimoCreationProgress(
                           coroutineManager,
                           key,
                           () =>
                           {
                               progressPercentage = 100;
                               FetchPreview(updateDelegate, coroutineManager);
                           },
                           progress =>
                           {
                               if (progressPercentage != (int)progress)
                               {
                                   progressPercentage = (int)progress;
                                   updateDelegate(this);
                               }
                           },
                           exception =>
                           {
                               isDone = true;
                               failed = true;
                               progressPercentage = 100;
                               Debug.LogError("Failed to check the progress of Didimo creation.");
                           });

                    yield return new WaitForSecondsRealtime(progressUpdateInterval);
                }
            }
        }


        /// <summary>
        /// Show the Didimo Window
        /// </summary>
        //[MenuItem("Window/Didimo/Didimo Plugin Window")]
        [MenuItem("Window/Didimo/Didimo Browser ")]
        public static void ShowWindow()
        {
            DidimoWindow window = (DidimoWindow)EditorWindow.GetWindow(typeof(DidimoWindow));
            window.minSize = new Vector2(300, 250);
            window.titleContent = new GUIContent("Didimo Browser");
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            bool shouldUpdateProfile = didimoPreviewView == null && ConnectionState == APIConnectionState.Connected;

            if (didimoThumbnailScrollview == null)
            {
                didimoThumbnailScrollview = CreateInstance<DidimoWindowThumbnailsView>();
                didimoThumbnailScrollview.hideFlags = HideFlags.HideAndDontSave;
                didimoPreviewView = CreateInstance<DidimoWindowPreviewView>();
                didimoPreviewView.hideFlags = HideFlags.HideAndDontSave;
                didimoThumbnailScrollview.SetPreviewView(didimoPreviewView);
                didimoSetupAuthKeyView = CreateInstance<DidimoWindowSetupAuthKeyView>();
                didimoSetupAuthKeyView.hideFlags = HideFlags.HideAndDontSave;

                iconFont = Resources.Load<Font>("ElegantIcons");
            }

            //Delegates are not serialized, so we will always set them on each enable

            didimoPreviewView.Init(
                () =>
            {
                Repaint();
            });

            didimoThumbnailScrollview.Init(
                   didimo =>
                   {
                       //if (didimo != null)
                       //{
                       didimoPreviewView.PreviewDidimo(didimo);
                       if (didimo != null)
                           isDeformationSupported = DidimoAvatarDataObject.IsDeformationSupported(didimo.meta);
                       //}
                   },
                   selectedIndices =>
                   {
                       StartDownloadProcess();
                   },
                   () =>
                   {
                       Repaint();
                   },
                   e =>
                   {
                       SendEvent(e);
                   }
                   );

            didimoSetupAuthKeyView.Init(
                (profileDataObject) =>
                {
                    if (profileDataObject != null)
                    {
                        Debug.Log("STATUS: CONNECTED");
                        ConnectionState = APIConnectionState.Connected;
                        RefreshAction();
                    }
                    else
                    {
                        Debug.Log("STATUS: DISCONNECTED - error obtaining profile");
                        ConnectionState = APIConnectionState.Disconnected;
                    }

                    Repaint();
                });

            if (shouldUpdateProfile)
            {
                EditorUtility.DisplayProgressBar("Refreshing", "Refreshing your profile, please wait.", 1);
                RefreshAction();
            }
        }

        void OnDestroy()
        {
            if (didimoCollectionFetcher != null)
                didimoCollectionFetcher.Stop();
            //As the thumbnail view might have a huge number of didimo thumbnails to download, lets destroy it when we don't need it anymore
            DestroyImmediate(didimoThumbnailScrollview);
        }

        APIConnectionState previousConnectionState;
        float connectionAttemptStartTime;
        void OnGUI()
        {
            //ConnectionState = APIConnectionState.Disconnected;
            //Debug.Log("OnGui - ConnectionState: " + ConnectionState);
            switch (ConnectionState)
            {
                case APIConnectionState.NotSetup:
                    previousConnectionState = ConnectionState;
                    didimoSetupAuthKeyView.Draw();
                    break;
                case APIConnectionState.Disconnected:
                    previousConnectionState = ConnectionState;
                    didimoSetupAuthKeyView.Draw();
                    break;
                case APIConnectionState.Connecting:
                    //disconnect on 10s timeout
                    if (previousConnectionState != ConnectionState)
                    {
                        connectionAttemptStartTime = Time.realtimeSinceStartup;
                        previousConnectionState = ConnectionState;
                    }
                    else if (Time.realtimeSinceStartup - connectionAttemptStartTime > 10f)
                        ConnectionState = APIConnectionState.Disconnected;
                    break;
                case APIConnectionState.Connected:
                    previousConnectionState = ConnectionState;
                    ShowDidimoManagementScreen();
                    break;
            }
        }

        void UpdateProfile()
        {
            ServicesRequests.EditorInstance.Profile(
                new EditorCoroutineManager(),
                profile =>
            {
                userProfile = profile;
                ConnectionState = APIConnectionState.Connected;
            },
                exception =>
            {
                EditorUtility.DisplayDialog("Failed to get profile", exception.Message, "OK");
                EditorUtility.ClearProgressBar();
                ConnectionState = APIConnectionState.Disconnected;
                Repaint();
            });
        }

        public void RefreshAction()
        {
            if (didimoCollectionFetcher != null)
                didimoCollectionFetcher.Stop();
            UpdateProfile();
            InitializeGallery();//maybe should wait for profile callback
        }

        #region PAGINATED LIST

        public void InitializeGallery()
        {
            didimos = new List<DidimoDataObject>();
            didimoThumbnailScrollview.ResetPagination();

            didimoCollectionFetcher = new DidimoGalleryCollectionFetcher(
                new EditorCoroutineManager(),
                nextPageDidimos =>
                {
                    waitingForNextPage = false;
                    didimoThumbnailScrollview.UpdateDidimosPaginated(nextPageDidimos);
                    EditorUtility.ClearProgressBar();
                    ConnectionState = APIConnectionState.Connected;
                },
                (updatedDidimos, removedDidimos) =>
                {
                    foreach (string removedDidimoKey in removedDidimos)
                    {
                        DidimoDataObject ddidimo = didimos.Find(didimo => didimo.key.Equals(removedDidimoKey));
                        if (ddidimo != null)
                        {
                            didimos.Remove(ddidimo);
                        }
                    }

                    didimoThumbnailScrollview.RemoveDidimos(removedDidimos);

                    didimoThumbnailScrollview.AddUpdatingDidimosPaginated(updatedDidimos);

                    EditorUtility.ClearProgressBar();
                    ConnectionState = APIConnectionState.Connected;
                });
        }

        public void FetchMore()
        {
            if (didimoCollectionFetcher.GetNumberOfElements() == didimoThumbnailScrollview.GetNumberOfElements())
            {
                EditorUtility.DisplayDialog("Info", "There are no more didimos in your account!", "OK");
                return;
            }
            else if (didimoCollectionFetcher.IsFetchMoreRequestPending())
            {
                EditorUtility.DisplayDialog("Error", "Please wait for the previous request to be completed before requesting more results!", "OK");
                return;
            }
            else didimoCollectionFetcher.FetchNextPage();
        }

        #endregion

        void CreateNewDidimo()
        {
            if (userProfile != null && userProfile.points <= 0)
            {
                if (EditorUtility.DisplayDialog("Not enough points", "You must top up your account to create more didimos. You can do this in our customer portal. Do you wish to be redirected to " + ServicesRequestsConfiguration.DefaultConfig.CustomerPortalUrl + "?",
                    "Yes",
                    "Cancel"))
                {
                    Application.OpenURL(ServicesRequestsConfiguration.DefaultConfig.CustomerPortalUrl);
                }
            }
            else
            {
                string photoPath = EditorUtility.OpenFilePanel("Select a Photo", "", "jpg,png,jpeg");
                if (photoPath.Length != 0)
                {
                    ServicesRequests.EditorInstance.CreateDidimo(
                        new EditorCoroutineManager(),
                        photoPath,
                        didimoCode =>
                        {
                            UpdateProfile();
                        },
                        exception =>
                        {
                            EditorUtility.DisplayDialog("Failed to create didimo", exception.Message, "OK");
                            EditorUtility.ClearProgressBar();
                        });
                }
            }
        }

        void ShowDidimoManagementScreen()
        {
            if (didimoCollectionFetcher == null)
            {
                RefreshAction();
                return;
            }

            Rect rect = new Rect(0, 0, position.width, headerHeight);
            GUILayout.BeginArea(rect, GUI.skin.scrollView);
            GUILayout.BeginHorizontal();

            GUIStyle centeredBoldLabel = new GUIStyle(EditorStyles.label);
            centeredBoldLabel.font = EditorStyles.boldLabel.font;
            centeredBoldLabel.alignment = TextAnchor.MiddleCenter;
            GUIStyle centeredLabel = new GUIStyle(EditorStyles.label);
            centeredLabel.alignment = TextAnchor.MiddleCenter;

            GUILayout.Button("Didimos", centeredBoldLabel);

            GUILayout.Space(5);
            if (GUILayout.Button("+", centeredLabel))
            {
                CreateNewDidimo();
            }

            GUILayout.FlexibleSpace();

            if (userProfile != null)
            {
                GUILayout.Label(string.Format("{0} points", userProfile.points));
            }

            GUILayout.Space(2);
            GUIStyle didimoMenuButtonStyle = new GUIStyle(GUI.skin.label);
            didimoMenuButtonStyle.font = iconFont;
            didimoMenuButtonStyle.normal.textColor = Color.black;
            didimoMenuButtonStyle.fontSize = geariconFontSize;
            didimoMenuButtonStyle.alignment = TextAnchor.MiddleCenter;
            char gearChar = '\xe037';

            if (GUILayout.Button(gearChar.ToString(), didimoMenuButtonStyle))
            {

                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Didimo"), false, () =>
                {
                    CreateNewDidimo();
                });
                menu.AddItem(new GUIContent("Refresh"), false, () =>
                {
                    EditorUtility.DisplayProgressBar("Refreshing", "Refreshing profile. Please wait.", 1);
                    RefreshAction();
                });

                menu.AddItem(new GUIContent("About"), false, () =>
                {
                    //Application.OpenURL(ServicesRequestsConfiguration.DefaultConfig.didimoForDevelopersUrl);
                    Application.OpenURL("https://mydidimo.com/about");
                });

                menu.AddSeparator("");
                menu.AddDisabledItem(new GUIContent(userProfile != null ? userProfile.tier_description : "User account"));
                menu.AddItem(new GUIContent("Setup API Key"), false, () =>
                {
                    ServicesRequests.EditorInstance.Logout();
                    ConnectionState = APIConnectionState.Disconnected;
                    didimoSetupAuthKeyView.state = ConnectionState;
                    didimoPreviewView.PreviewDidimo(null);

                    if (didimoCollectionFetcher != null)
                        didimoCollectionFetcher.Stop();
                    Repaint();
                });
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Rect scrollviewVisibleRect = new Rect(0, headerHeight, scrollViewWidth + GUI.skin.verticalScrollbar.fixedWidth, position.height - headerHeight - loadMoreHeight);
            didimoThumbnailScrollview.DrawDidimoThumbnails(scrollviewVisibleRect);

            EditorGUI.BeginDisabledGroup(didimoCollectionFetcher.IsFetchMoreRequestPending() || didimoCollectionFetcher.GetNumberOfElements() == didimoThumbnailScrollview.GetNumberOfElements());
            GUIStyle bigButtonStyle2 = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button);
            bigButtonStyle2.fixedWidth = scrollViewWidth + GUI.skin.verticalScrollbar.fixedWidth - 15;
            bigButtonStyle2.fontSize = 14;
            if (GUILayout.Button("More", bigButtonStyle2, GUILayout.Width(scrollViewWidth + GUI.skin.verticalScrollbar.fixedWidth - 15)))
            {
                FetchMore();
            }
            EditorGUI.EndDisabledGroup();




            Rect previewAreaRect = new Rect(scrollviewVisibleRect.width, headerHeight, position.width - scrollviewVisibleRect.width, position.height - headerHeight);
            didimoPreviewView.DrawPreview(previewAreaRect);

            GUILayout.BeginArea(previewAreaRect);
            bool shouldInteractionBeEnabled = didimoThumbnailScrollview.LastSelectedDidimo != null && !didimoThumbnailScrollview.LastSelectedDidimo.hasFailed && didimoThumbnailScrollview.LastSelectedDidimo.isDone;
            GUI.enabled = shouldInteractionBeEnabled;
            GUIStyle bigButtonStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button);
            bigButtonStyle.fontSize = 14;
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (didimoThumbnailScrollview.LastSelectedDidimo != null && didimoThumbnailScrollview.LastSelectedDidimo.hasUnityTarget)
            {
                if (GUILayout.Button("Import", bigButtonStyle, GUILayout.Width(125)))
                {
                    StartDownloadProcess();
                }
            }
            else GUILayout.Label("Unity package is unavailable for this didimo", GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(2);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        void StartDownloadProcess()
        {
            List<DidimoStatusDataObject> didimosToDownload = didimoThumbnailScrollview.SelectedDidimosAvailableToImport();

            foreach (DidimoStatusDataObject didimoToDownload in didimosToDownload)
            {
                if (didimoToDownload.template_version != "2.0")
                {
                    EditorUtility.DisplayDialog("Cannot download didimo",
                        "Unsupported didimo template selected: '" + didimoToDownload.template_version + "'. Please select a didimo generated with 2.0 template.",
                        "");
                    return;
                }
            }

            string saveFolder = null;
            bool proceed = false;
            while (!proceed)
            {
                saveFolder = EditorUtility.SaveFolderPanel("Select the folder", "Assets", "");

                if (string.IsNullOrEmpty(saveFolder) || saveFolder.Contains(Application.dataPath))
                {
                    proceed = true;
                }
                else
                {
                    saveFolder = string.Empty;

                    if (!EditorUtility.DisplayDialog("Error", "Please select a path inside the project's asset folder.", "OK", "Cancel"))
                    {
                        proceed = true;
                    }
                }
            }

            if (saveFolder.Length != 0)
            {
                EditorCoroutineManager coroutineManager = new EditorCoroutineManager();
                coroutineManager.StartCoroutine(DownloadSelectedDidimos(saveFolder));
            }
        }

        IEnumerator DownloadSelectedDidimos(string saveFolder)
        {
            if (coroutineManager == null)
            {
                coroutineManager = new EditorCoroutineManager();
            }
            else
            {
                coroutineManager.StopAllCoroutines();
            }

            List<DidimoStatusDataObject> didimosToDownload = didimoThumbnailScrollview.SelectedDidimosAvailableToImport();

            float totalDidimosToDownload = didimosToDownload.Count;
            EditorUtility.DisplayProgressBar("Downloading files", "Downloading your didimo files, please wait", 0);
            yield return new WaitForSecondsRealtime(0f);

            bool shouldClearProgress = false;
            while (didimosToDownload.Count > 0)
            {
                DidimoStatusDataObject didimoModel = didimosToDownload[0];
                bool error = false;

                string path = saveFolder;
                if (totalDidimosToDownload > 1)
                {
                    path = Path.Combine(saveFolder, didimoModel.key);
                }

                string unzipFolder = Path.Combine(Application.temporaryCachePath, didimoModel.key);
                Directory.CreateDirectory(unzipFolder);
                Debug.Log(unzipFolder);

                yield return ServicesRequests.EditorInstance.FetchDidimoZip(new EditorCoroutineManager(),
                     "unity",
                     didimoModel.key,
                     (zippath, zipBytes) =>
                     {
                         didimosToDownload.RemoveAt(0);
                         EditorUtility.DisplayProgressBar("Downloading files", "Downloading didimo files, please wait", (totalDidimosToDownload - didimosToDownload.Count) / totalDidimosToDownload);

                         DidimoFilesManager.SaveToFolder(unzipFolder, zipBytes);

                         ImportModel(Path.Combine(unzipFolder, "Model"), didimoModel.key, path);
                         Directory.Delete(unzipFolder, true);
                     },
                     exception =>
                     {
                         EditorUtility.DisplayDialog("Download failed", "Failed to download didimo. Please try again later.", "OK");
                         EditorUtility.ClearProgressBar();
                         if (Directory.Exists(unzipFolder))
                         {
                             Directory.Delete(unzipFolder, true);
                         }
                         error = true;
                     });

                //Apply deformation to all hair prefabs and save to folder
                if (DidimoAvatarDataObject.IsDeformationSupported(didimoModel.meta))
                {

                    if (EditorUtility.DisplayDialog("Hairstyles available", "You can download hairstyles fitted to this didimo. Would you like to do this now?",
                    "Yes",
                    "No"))
                    {
                        EditorUtility.DisplayProgressBar("Processing Hair Assets", "Retrieving list of hairstyles, please wait", 0);
                        yield return new WaitForSecondsRealtime(0f);
                        DownloadHairstyles(didimoModel, path);
                    }
                }
                else shouldClearProgress = true;

                if (error)
                {
                    yield break;
                }
            }
            Repaint();
            if (shouldClearProgress)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        ~DidimoWindow()
        {
            if (coroutineManager != null)
            {
                coroutineManager.StopAllCoroutines();
            }
        }


        private void ImportModel(string sourceFolder, string didimoKey, string path)
        {
            try
            {
                string modelPath = Path.Combine(sourceFolder, "avatar_model.json");
                EditorUtility.DisplayProgressBar("Importing Didimo", "Deserializing...", 0);
                string modelFolder = Path.GetDirectoryName(modelPath);

                GameObject didimo = new GameObject();
                didimo.name = didimoKey;

                try
                {
                    StreamReader reader = new StreamReader(modelPath);
                    IDidimoModel model = DidimoModelFactory.CreateDidimoModel(reader.ReadToEnd());
                    reader.Close();
                    EditorUtility.DisplayProgressBar("Importing Didimo", "Importing model...", 1);
                    model.ImportDidimo(didimo.name, didimo.transform, sourceFolder, path);

                    // Copy license file
                    string licenseFile = Path.Combine(sourceFolder, "LICENSE.txt");
                    if (File.Exists(licenseFile))
                    {
                        File.Copy(licenseFile, Path.Combine(path, "LICENSE.txt"), true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
                //finally
                //{
                //    DestroyImmediate(didimo);
                //}
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #region HAIR

        private int hairstyles_progress = 0;
        void DownloadHairstyles(DidimoStatusDataObject didimoModel, string saveToPath)
        {
            string didimoCode = didimoModel.key;

            string[] hairPrefabs = {
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_001.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_006.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_007.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_008.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_009.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_010.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_011.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_012.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_013.prefab",
                                "Assets/Didimo/Hair/Template_2.0/Prefabs/Hairstyle_014.prefab"};

            int total_items = hairPrefabs.Length + 1;
            hairstyles_progress = 0;
            EditorUtility.DisplayProgressBar("Processing Hair Assets", "Processing your Didimo Files, please wait", hairstyles_progress / total_items);

            Vector3 hairsocket = new Vector3(0f, 0f, 0f);
            foreach (string prefab_path in hairPrefabs)
            {
                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath(prefab_path, (typeof(GameObject))) as GameObject;
                if (prefab != null)
                {
                    GameObject currentHair = Instantiate(prefab, hairsocket, Quaternion.identity) as GameObject;
                    //currentHair.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    currentHair.SetActive(false);
                    DeformHair(currentHair, didimoCode,
                        deformedHair =>
                        {
                            ExportToOBJ(didimoCode, saveToPath, deformedHair);
                            DestroyImmediate(currentHair);
                            hairstyles_progress++;
                            if (hairstyles_progress == total_items)
                            {
                                EditorUtility.ClearProgressBar();
                            }
                            else
                            {
                                EditorUtility.DisplayProgressBar("Processing Hair Assets", "Processing your Didimo Files, please wait", ((float)hairstyles_progress / total_items));
                            }
                        });
                }
                else
                {
                    Debug.LogError("Asset not found at: " + prefab_path);
                    EditorUtility.ClearProgressBar();
                    Debug.LogError("Aborting...");
                    return;
                }
            }

            AssetDatabase.Refresh();
        }


        /**************************************************************************/
        /**** Hair deformation ****************************************************/
        /**************************************************************************/
        //object o;
        void DeformHair(GameObject currentHair, string didimoCode, Action<GameObject> successDelegate)
        {
            byte[] vertexBytes = WriteCurrentVertexPositionsToBinary(currentHair);

            coroutineManager.StartCoroutine(ServicesRequests.EditorInstance.DeformDidimoAssetAsync_Editor(
                      coroutineManager,
                      didimoCode,
                      currentHair.name,
                      vertexBytes,
                      (newVertexBytes) =>
                      {
                          //Debug.Log("DeformDidimoAsset - response received " + newVertexBytes.Length);
                          currentHair = ReadCurrentVertexPositionsToBinary(currentHair, newVertexBytes);
                          successDelegate(currentHair);
                      },
                      (progress) =>
                      {
                          //Debug.Log("Deformation progress (%): " + progress);
                      },
                      exception =>
                      {
                          coroutineManager.StopAllCoroutines();
                          Debug.LogError(exception.Message);
                      }));
        }

        byte[] WriteCurrentVertexPositionsToBinary(GameObject currentHair)
        {
            byte[] vertex_bytes = null;
            try
            {
                Stream stream = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    float jsonScaleFactor = 0.01f;
                    MeshFilter[] meshList = currentHair.GetComponentsInChildren<MeshFilter>();
                    Vector3[] vertexList;
                    int byte_length = 0;

                    foreach (MeshFilter mf in meshList)
                    {
                        vertexList = mf.sharedMesh.vertices;
                        for (int i = 0; i < vertexList.Length; i++)
                        {
                            vertexList[i].x *= -1;
                            vertexList[i] /= jsonScaleFactor;
                            byte[] xValueByteArray = BitConverter.GetBytes(vertexList[i].x);
                            byte[] yValueByteArray = BitConverter.GetBytes(vertexList[i].y);
                            byte[] zValueByteArray = BitConverter.GetBytes(vertexList[i].z);
                            if (!BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(xValueByteArray); // Convert big endian to little endian
                                Array.Reverse(yValueByteArray); // Convert big endian to little endian
                                Array.Reverse(zValueByteArray); // Convert big endian to little endian
                            }
                            bw.Write(xValueByteArray);
                            bw.Write(yValueByteArray);
                            bw.Write(zValueByteArray);
                            byte_length += 3 * 4;
                        }
                    }

                    // Open a reader to make reading those values easy
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Position = 0;
                        vertex_bytes = reader.ReadBytes(byte_length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
            }

            return vertex_bytes;
        }

        GameObject ReadCurrentVertexPositionsToBinary(GameObject currentHair, byte[] newVertexBytes)
        {
            List<Vector3> newPositionsList = new List<Vector3>();
            Stream s = new MemoryStream(newVertexBytes);
            BinaryReader br = new BinaryReader(s);
            byte[] bytes;
            float valueX;
            float valueY;
            float valueZ;
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                bytes = br.ReadBytes(4);
                valueX = floatConversion(bytes);
                bytes = br.ReadBytes(4);
                valueY = floatConversion(bytes);
                bytes = br.ReadBytes(4);
                valueZ = floatConversion(bytes);
                newPositionsList.Add(new Vector3(valueX, valueY, valueZ));
            }

            float jsonScaleFactor = 0.01f;

            MeshFilter[] meshList = currentHair.GetComponentsInChildren<MeshFilter>();
            Vector3[] vertexList;

            int index = 0;
            foreach (MeshFilter mf in meshList)
            {
                Mesh mesh = Instantiate(mf.sharedMesh);
                mesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                vertexList = mesh.vertices;
                for (int i = 0; i < vertexList.Length; i++)
                {
                    vertexList[i] = newPositionsList[index++];
                    vertexList[i].x *= -1;
                    vertexList[i] *= jsonScaleFactor;
                }
                mesh.vertices = vertexList;
                mf.sharedMesh = mesh;
            }
            return currentHair;
        }

        public float floatConversion(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes); // Convert big endian to little endian
            }
            float myFloat = BitConverter.ToSingle(bytes, 0);
            return myFloat;
        }

        /**************************************************************************/
        /**** EXPORTER ************************************************************/
        /**************************************************************************/

        void ExportToOBJ(string didimoCode, string path, GameObject objMeshToExport)
        {
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            if (!Directory.Exists(Path.GetDirectoryName(path + "/DeformedHair/")))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path + "/DeformedHair/"));
            }
            if (!Directory.Exists(Path.GetDirectoryName(path + "/HairPrefabs/")))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path + "/HairPrefabs/"));
            }

            string relativeFolderPath = path.Replace(Application.dataPath, "Assets");

            GameObject newCurrentHairPrefab = new GameObject();
            newCurrentHairPrefab.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            newCurrentHairPrefab.SetActive(false);
            newCurrentHairPrefab.name = objMeshToExport.name.Replace("(Clone)", "") + "_" + didimoCode;

            //export to obj
            MeshFilter[] meshList = objMeshToExport.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter mf in meshList)
            {
                string meshNameCleaned = mf.sharedMesh.name.Replace(":", "_");
                string path2 = path + "/DeformedHair/" + newCurrentHairPrefab.name + "_" + meshNameCleaned + ".obj";
                ObjExporter.MeshToFile(mf, path2);
            }

            AssetDatabase.Refresh(); //refresh to get the newly added assets

            //create prefab that uses all of the meshes+materials in the correct positions
            foreach (MeshFilter mf in meshList)
            {
                string meshNameCleaned = mf.sharedMesh.name.Replace(":", "_");
                Material meshMaterial = mf.transform.GetComponent<MeshRenderer>().sharedMaterial;

                UnityEngine.Object sub_hair_prefab = AssetDatabase.LoadAssetAtPath(relativeFolderPath + "/DeformedHair/" + newCurrentHairPrefab.name + "_" + meshNameCleaned + ".obj", (typeof(GameObject))) as GameObject;
                GameObject currentHair2Prefab_temp = Instantiate(sub_hair_prefab, Vector3.zero, Quaternion.identity) as GameObject;
                currentHair2Prefab_temp.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                MeshRenderer mr = currentHair2Prefab_temp.GetComponentInChildren<MeshRenderer>();
                mr.sharedMaterial = meshMaterial;
                currentHair2Prefab_temp.transform.SetParent(newCurrentHairPrefab.transform);
            }

            DoCreatePrefab(path + "/HairPrefabs/", newCurrentHairPrefab);
            DestroyImmediate(newCurrentHairPrefab);
        }

        public static void DoCreatePrefab(string path, GameObject objectToPrefab)
        {
            string relativeFolderPath = path.Replace(Application.dataPath, "Assets");
            objectToPrefab.SetActive(true);
            UnityEngine.Object prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(objectToPrefab, relativeFolderPath + objectToPrefab.name + ".prefab");
            objectToPrefab.SetActive(false);
            prefab.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            //Debug.Log("Prefab Created at: " + relativeFolderPath + objectToPrefab.name + ".prefab");
        }

        #endregion

    }

}
