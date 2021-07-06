using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils;
using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Window
{
    /// <summary>
    /// The purpose of this class is to draw the Didimo previews for the Didimo Window.
    /// </summary>
    /// 
    [System.Serializable]
    public class DidimoWindowPreviewView : ScriptableObject
    {

        const int logoHeight = 16;

        System.Action repaintDelegate;

        [SerializeField]
        PreviewType selectedPreviewPerspective = PreviewType.FrontPerspective;
        [SerializeField]
        Texture2D didimoLogoTexture;
        [SerializeField]
        Texture2D previewAreaBackgroundTexture;
        [SerializeField]
        DidimoPreview didimo;
        EditorCoroutineManager coroutineManager;
        bool failedToDownload = false;

        void OnEnable()
        {
            if (previewAreaBackgroundTexture == null)
                ClearPreview();
        }

        public void ClearPreview()
        {
            previewAreaBackgroundTexture = null;
            previewAreaBackgroundTexture = TextureUtils.TextureFromColor(Color.black);
            previewAreaBackgroundTexture.hideFlags = HideFlags.HideAndDontSave;
            didimoLogoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Didimo/Sprites/Menu/didimo_logo_V3_crop.png");
        }

        /// <summary>
        /// DidimoWindowPreviewView constructor.
        /// </summary>
        /// <param name="repaintDelegate">This delegate will be called if a <see cref="EditorWindow.Repaint"/> is needed.</param>
        public void Init(System.Action repaintDelegate)
        {
            this.repaintDelegate = repaintDelegate;
        }

        /// <summary>
        /// Set the current didimo to be previewed.
        /// </summary>
        /// <param name="didimoStatus">The didimo to be previewed.</param>
        public void PreviewDidimo(DidimoStatusDataObject didimoStatus, bool force = false)
        {
            if (didimoStatus == null)
            {
                //Debug.Log("FORCING null");
                didimo = null;
                repaintDelegate();
            }
            else if (force && didimo != null)
            {
                //Debug.Log("FORCING");
                didimo = new DidimoPreview(didimoStatus);
                failedToDownload = false;
                didimo.isDownloading = false;
                didimo.status.status = "done";
                didimo.preview = null;
                repaintDelegate();
            }
            else if (didimo == null || !didimo.status.key.Equals(didimoStatus.key))
            {
                didimo = new DidimoPreview(didimoStatus);
                failedToDownload = false;
                repaintDelegate();
            }
            else //if(didimo == null && )
            {
                //didimo.status.Failed = false;
                //didimo.status.done = true;// repaintDelegate(); //Debug.Log("Another 3");
            }
        }


        /// <summary>
        /// Draw the preview view.
        /// </summary>
        /// <param name="previewAreaRect">The rect where the preview will be drawn.</param>
        public void DrawPreview(Rect previewAreaRect)
        {
            GUIStyle previewAreaStyle = new GUIStyle(GUIStyle.none);
            previewAreaStyle.normal.background = previewAreaBackgroundTexture;
            GUILayout.BeginArea(previewAreaRect, previewAreaStyle);

            if (didimo != null && didimo.preview != null && didimo.status.isDone)
            {
                Rect didimoPreviewRect = previewAreaRect;

                float maxWidth = didimo.preview.width;
                float maxHeight = didimo.preview.height;
                float widthOverflow = Mathf.Max(0, didimoPreviewRect.width - maxWidth);
                float heightOverflow = Mathf.Max(0, didimoPreviewRect.height - maxHeight);

                didimoPreviewRect.x = widthOverflow / 2f;
                didimoPreviewRect.y = heightOverflow / 2f;
                didimoPreviewRect.width -= widthOverflow;
                didimoPreviewRect.height -= heightOverflow;

                GUI.DrawTexture(didimoPreviewRect, didimo.preview, ScaleMode.ScaleToFit);
            }

            bool shouldInteractionBeEnabled = didimo != null && !didimo.status.hasFailed && didimo.status.isDone;

            GUILayout.BeginVertical();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = shouldInteractionBeEnabled;
            //PreviewType previousPerspective = selectedPreviewPerspective;
            /*selectedPreviewPerspective = (PreviewType)GUILayout.Toolbar((int)selectedPreviewPerspective, new string[] {
                "Left",
                "Front",
                "Corner"
            });*/

            if (didimo != null && didimo.status.hasFailed == false && didimo.status.isDone)
            {
                if ((!didimo.isDownloading && didimo.preview == null && !failedToDownload) /*|| previousPerspective != selectedPreviewPerspective*/)
                {
                    //Debug.Log("DownloadPreview has been temp disabled. ");
                    DownloadPreview(
                     selectedPreviewPerspective,
                     texture =>
                     {
                         if (texture != null)
                         {
                             if (didimo != null)
                             {
                                 didimo.preview = texture;
                                 didimo.preview.hideFlags = HideFlags.HideAndDontSave;
                             }
                             repaintDelegate();
                         }
                     });
                }
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int logoWidth = logoHeight * didimoLogoTexture.width / didimoLogoTexture.height;
            GUILayout.Label(didimoLogoTexture, GUILayout.Height(logoHeight), GUILayout.Width(logoWidth));
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUIStyle bigButtonStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button);
            bigButtonStyle.fontSize = 14;

            if (didimo != null || failedToDownload)
            {
                string message = null;
                MessageType messageType = MessageType.None;

                if (failedToDownload)
                {
                    message = "There was an error downloading your Didimo. Please try again later. ";
                    messageType = MessageType.Error;
                }
                else if (!didimo.status.isDone)
                {
                    message = "We are generating your didimo. Please wait. ";
                    messageType = MessageType.Info;
                }
                else if (didimo.status.hasFailed)
                {
                    message = "There was an error processing this photo.\nPlease try again with another photo. ";
                    messageType = MessageType.Error;
                }
                else if (didimo.isDownloading)
                {
                    message = "Downloading preview. Please wait. ";
                    messageType = MessageType.None;
                }

                if (message != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox(message, messageType);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(50);
                }
            }

            GUILayout.EndArea();
        }

        string PreviewUrlFromType(PreviewType previewType)
        {
            return "front.png";
            /*switch (previewType)
            {
                case PreviewType.SidePerspective:
                    return "left.png";
                case PreviewType.FrontPerspective:
                    return "front.png";
                case PreviewType.CornerPerspective:
                    return "right.png";
            }
            return null;*/
        }

        void DownloadPreview(PreviewType previewType, System.Action<Texture2D> doneAction)
        {
            didimo.isDownloading = true;
            if (coroutineManager != null)
            {
                coroutineManager.StopAllCoroutines();
            }
            else
            {
                coroutineManager = new EditorCoroutineManager();
            }

            failedToDownload = false;

            ServicesRequests.EditorInstance.DownloadDidimoPreview(
                coroutineManager,
                previewType,
                didimo.status.key,
                texture =>
                {
                    if (didimo != null)
                        didimo.isDownloading = false;
                    failedToDownload = false;
                    if (didimo != null)
                        doneAction(texture);
                },
                error =>
                {
                    if (didimo != null)
                        didimo.isDownloading = false;
                    failedToDownload = true;
                    repaintDelegate();
                });
        }

        ~DidimoWindowPreviewView()
        {
            if (coroutineManager != null)
            {
                coroutineManager.StopAllCoroutines();
            }
        }

        [System.Serializable]
        private class DidimoPreview
        {
            [SerializeField]
            public DidimoStatusDataObject status;
            [System.NonSerialized]
            public bool isDownloading;
            [SerializeField]
            public Texture2D preview;

            public DidimoPreview(DidimoStatusDataObject didimoStatus)
            {
                status = didimoStatus;
                isDownloading = false;
            }
        }
    }
}