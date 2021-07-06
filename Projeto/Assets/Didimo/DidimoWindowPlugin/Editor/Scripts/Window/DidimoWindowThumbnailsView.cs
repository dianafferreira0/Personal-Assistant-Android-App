using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils;
using Didimo.Utils.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Window
{
    /// <summary>
    /// The purpose of this class is to draw the didimo thumbnail scroll view for the Didimo Window.
    /// </summary>
    [System.Serializable]
    public class DidimoWindowThumbnailsView : ScriptableObject
    {
        const int lockiconFontSize = 17;
        const float thumbnailMargins = 1;
        static readonly Vector2 lockIconMargins = new Vector2(2f, 2f);

        /// <summary>
        /// The list of the selected didimo indices.
        /// </summary>
        public List<int> selectedDidimoIndices;

        private DidimoWindowPreviewView didimoPreviewView;

        System.Action<DidimoStatusDataObject> didSelectDidimoDelegate; 
        System.Action<List<int>> buyAndImportSelectedItemsDelegate;
        System.Action repaintDelegate;
        System.Action<Event> sendEventDelegate;
        EditorCoroutineManager coroutineManager;

        [SerializeField]
        List<Rect> thumbnailRects;
        [SerializeField]
        float thumbnailSize;
        [SerializeField]
        Font iconFont;
        [SerializeField]
        Texture2D thumbnailPlaceholderTexture;
        [SerializeField]
        Texture2D listItemSelectedTexture;
        [SerializeField]
        Vector2 didimoThumbnailsScrollPosition;
        [SerializeField]
        int lastSelectedItemIndex;
        [SerializeField]
        Rect visibleRect;
        [SerializeField]
        Rect viewRect;
        [SerializeField]
        List<DidimoThumbnail> didimos;
        Vector2? lastEventMousePosition = null;

        void OnEnable()
        {
            if (didimos == null)
            {
                didimos = new List<DidimoThumbnail>();
                iconFont = Resources.Load<Font>("ElegantIcons");
                thumbnailPlaceholderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Didimo/Editor/Sprites/PhotoTemplate.png");
                thumbnailPlaceholderTexture.hideFlags = HideFlags.DontSave;
                listItemSelectedTexture = TextureUtils.TextureFromColor(new Color(89 / 255f, 137 / 255f, 207 / 255f));
                listItemSelectedTexture.hideFlags = HideFlags.HideAndDontSave;
                thumbnailRects = new List<Rect>();
                selectedDidimoIndices = new List<int>();
            }

            coroutineManager = new EditorCoroutineManager();
            UpdateDidimoThumbnailsIfNeeded();
        }

        private void OnDestroy()
        {
            foreach (DidimoThumbnail didimo in didimos)
            {
                didimo.StopCreationProcessUpdate();
            }

            if (coroutineManager != null)
            {
                coroutineManager.StopAllCoroutines();
            }
        }

        public float GetVisibleRectHeight()
        {
            return visibleRect.height;
        }

        public void SetPreviewView(DidimoWindowPreviewView didimoPreviewView)
        {
            this.didimoPreviewView = didimoPreviewView;
        }


    /// <summary>
    /// Init the DidimoWindowThumbnailsView with the proper delegates.
    /// </summary>
    /// <param name="didSelectDidimoDelegate">The delegate to be called each time a didimo is selected.</param>
    /// <param name="buyAndImportSelectedItemsDelegate">The delegate to be called when the "buy and import" button is pressed.</param>
    /// <param name="repaintDelegate">The delegate to be called when a <see cref="EditorWindow.Repaint"/> is needed.</param>
    /// <param name="sendEventDelegate">The delegate to be called when a <see cref="EditorWindow.SendEvent(Event)"/> need to be called.</param>
    public void Init(System.Action<DidimoStatusDataObject> didSelectDidimoDelegate, System.Action<List<int>> buyAndImportSelectedItemsDelegate, System.Action repaintDelegate, System.Action<Event> sendEventDelegate)
        {

            this.didSelectDidimoDelegate = didSelectDidimoDelegate;
            this.buyAndImportSelectedItemsDelegate = buyAndImportSelectedItemsDelegate;
            this.repaintDelegate = repaintDelegate;
            this.sendEventDelegate = sendEventDelegate;
        }

        /// <summary>
        /// The last selected didimo.
        /// </summary>
        public DidimoStatusDataObject LastSelectedDidimo
        {
            get
            {
                if (lastSelectedItemIndex == -1 || didimos.Count == 0)
                {
                    return null;
                }
                else
                {
                    return didimos[lastSelectedItemIndex].didimoModel;
                }
            }
        }

        /// <summary>
        /// Update the list of didimos to be displayed. (DEPRECATED)
        /// </summary>
        /// <param name="didimoModels">The new didimos to be displayed in the scrollview.</param>
        public void UpdateDidimos(DidimoStatusDataObject[] didimoModels)
        {
            StopAllCreationProgressUpdates();
            List<DidimoThumbnail> newDidimos = new List<DidimoThumbnail>();

            for (int i = 0; i < didimoModels.Length; i++)
            {
                DidimoStatusDataObject model = didimoModels[i];
                DidimoThumbnail didimo;
                didimo = didimos.Find(d => d.didimoModel.key.Equals(model.key));

                if (didimo == null)
                {
                    didimo = new DidimoThumbnail(model);
                }
                else
                {
                    didimo.didimoModel = model;
                }

                newDidimos.Add(didimo);
                //pass a new variable representing the index to the action
                int index = i;
                didimo.UpdateCreationProcessIfNeeded(() =>
                {
                    if (IsDidimoVisible(index))
                    {
                        repaintDelegate();
                    }
                });
            }
            didimos = newDidimos;
            List<int> selectedDidimoIndices = new List<int>();

            for (int i = 0; i < didimos.Count; i++)
            {
                if (this.selectedDidimoIndices.Contains(i))
                {
                    selectedDidimoIndices.Add(i);
                }
            }

            if (selectedDidimoIndices.Count == 0 && didimos.Count > 0)
            {
                selectedDidimoIndices.Add(0);
            }

            GenerateDidimoThumbnailRects();
            this.selectedDidimoIndices = selectedDidimoIndices;

            if (this.selectedDidimoIndices.Count > 0)
            {
                didSelectDidimoDelegate(didimos[this.selectedDidimoIndices[0]].didimoModel);
            }

            UpdateDidimoThumbnailsIfNeeded();
        }

        public void ResetPagination()
        {
            StopAllCreationProgressUpdates();
            didimos = new List<DidimoThumbnail>();
        }

        public int GetNumberOfElements()
        {
            return didimos.Count;
        }

        public void UpdateDidimosPaginated(List<DidimoAvatarDataObject> didimoModels)
        {
            int old_size = didimos.Count;
            for (int i = 0; i < didimoModels.Count; i++)
            {
                DidimoAvatarDataObject model_in = didimoModels[i];
                DidimoStatusDataObject model = new DidimoStatusDataObject();
                model.key = model_in.key;
                model.status = model_in.status; //model.done = model_in.done;
                model.updated_at = model_in.updatedAt;
                model.template_version = model_in.templateVersion;
                //model.failed = model_in.errorCode != 0;
                model.percent = (int)model_in.percent;
                model.meta = model_in.metadata;

                DidimoThumbnail didimo;
                didimo = didimos.Find(d => d.didimoModel.key.Equals(model.key));

                if (didimo == null)
                {
                    didimo = new DidimoThumbnail(model);
                }
                else
                {
                    didimo.didimoModel = model;
                }

                didimos.Add(didimo);
                //pass a new variable representing the index to the action
                int index = old_size + i; //i;
                didimo.UpdateCreationProcessIfNeeded(() =>
                {
                    if (IsDidimoVisible(index))
                    {
                        repaintDelegate();
                    }
                    if (this.selectedDidimoIndices.Count > 0)
                    {
                        DidimoStatusDataObject status = didimos[this.selectedDidimoIndices[0]].didimoModel;
                        if (status.isDone)
                        {
                            if (didimo.didimoModel.key.CompareTo(status.key) == 0)
                                didimoPreviewView.PreviewDidimo(status, true);
                        }
                    }
                });
            }
            //didimos = newDidimos;
            List<int> selectedDidimoIndices = new List<int>();

            for (int i = 0; i < didimos.Count; i++)
            {
                if (this.selectedDidimoIndices.Contains(i))
                {
                    selectedDidimoIndices.Add(i);
                }
            }

            if (selectedDidimoIndices.Count == 0 && didimos.Count > 0)
            {
                selectedDidimoIndices.Add(0);
            }

            GenerateDidimoThumbnailRects();
            this.selectedDidimoIndices = selectedDidimoIndices;

            if (this.selectedDidimoIndices.Count > 0)
            {
                didSelectDidimoDelegate(didimos[this.selectedDidimoIndices[0]].didimoModel);
            }

            UpdateDidimoThumbnailsIfNeeded();
        }

        public void AddUpdatingDidimosPaginated(List<DidimoAvatarDataObject> didimoModels)
        {
            int old_size = didimos.Count;
            List<int> selectedDidimoIndices = new List<int>();
            for (int i = 0; i < this.selectedDidimoIndices.Count; i++)
            {
                selectedDidimoIndices.Add(this.selectedDidimoIndices[i]);
            }

            for (int i = 0; i < didimoModels.Count; i++)
            {
                DidimoAvatarDataObject model_in = didimoModels[i];
                DidimoStatusDataObject model = new DidimoStatusDataObject();
                model.key = model_in.key;
                model.status = model_in.status; //model.done = model_in.done;
                model.updated_at = model_in.updatedAt;
                //model.failed = /*model_in.IsSuccess ||*/ model_in.errorCode != 0;
                model.template_version = model_in.templateVersion;
                model.percent = (int)model_in.percent;
                model.meta = model_in.metadata;

                DidimoThumbnail didimo;
                didimo = didimos.Find(d => d.didimoModel.key.Equals(model.key));

                int index = -1;

                
                if (didimo == null)
                {
                    didimo = new DidimoThumbnail(model);
                    didimos.Insert(0, didimo);

                    selectedDidimoIndices = new List<int>();
                    for (int i2 = 0; i2 < didimos.Count; i2++)
                    {
                        if (this.selectedDidimoIndices.Contains(i2))
                        {
                            int new_index = i2 + 1;
                            selectedDidimoIndices.Add(new_index);
                        }
                    }
                    index = 0; 
                }
                else
                {
                    didimo.didimoModel = model;
                    for (int i2 = 0; i2 < selectedDidimoIndices.Count; i2++)
                    {
                        if (didimos[selectedDidimoIndices[i2]].didimoModel.key.CompareTo(didimo.didimoModel.key) == 0)
                        {
                            index = i2;
                            
                        }
                    }
                }

                //pass a new variable representing the index to the action
                didimo.UpdateCreationProcessIfNeeded(() =>
                {
                    if (IsDidimoVisible(index))
                    {
                        repaintDelegate();
                    }
                    if (this.selectedDidimoIndices.Count > 0)
                    {
                        DidimoStatusDataObject status = didimos[this.selectedDidimoIndices[0]].didimoModel;
                        if (this.selectedDidimoIndices.Count > 0 && status.isDone)
                        {
                            if (didimo.didimoModel.key.CompareTo(status.key) == 0)
                                didimoPreviewView.PreviewDidimo(status, true);
                        }
                    }
                });
            }


            if (selectedDidimoIndices.Count == 0 && didimos.Count > 0)
            {
                selectedDidimoIndices.Add(0);
            }

            GenerateDidimoThumbnailRects();
            this.selectedDidimoIndices = selectedDidimoIndices;

            if (this.selectedDidimoIndices.Count > 0 && didimos.Count > this.selectedDidimoIndices[0])
            {
                didSelectDidimoDelegate(didimos[this.selectedDidimoIndices[0]].didimoModel);
            }

            UpdateDidimoThumbnailsIfNeeded();
        }

        public void RemoveDidimos(List<String> removedDidimos)
        {
            int totalDidimosToDelete = removedDidimos.Count;

            if (totalDidimosToDelete > 0)
            {
                int totalDidimosDeleted = 0;
                EditorUtility.DisplayProgressBar("Updating", "Didimos were deleted. Updating, please wait", (totalDidimosToDelete - totalDidimosDeleted) / totalDidimosToDelete);

                foreach (String key in removedDidimos)
                {
                    foreach (DidimoThumbnail didimo in didimos)
                    {
                        if (didimo.didimoModel.key.CompareTo(key) == 0)
                        {
                            didimos.Remove(didimo);
                            totalDidimosDeleted++;
                            EditorUtility.DisplayProgressBar("Updating", "Didimos were deleted. Updating, please wait", (totalDidimosToDelete - totalDidimosDeleted) / totalDidimosToDelete);
                            break;
                        }
                    }
                }

                lastSelectedItemIndex = -1;
                didSelectDidimoDelegate(null);
                thumbnailRects.Clear();
                GenerateDidimoThumbnailRects();
                EditorUtility.ClearProgressBar();
                repaintDelegate();
            }
        }


        /// <summary>
        /// Gets the index of the item the mouse is hovering.
        /// </summary>
        /// <returns>The index of the item the mouse is hovering. -1 if note hovering any item.</returns>
        public int GetHoveredItemIndex(Event e)
        {
            Vector2 mousePosition = new Vector2(e.mousePosition.x, e.mousePosition.y);
            mousePosition.y -= viewRect.y;
            mousePosition.x -= viewRect.x;
            mousePosition.y += didimoThumbnailsScrollPosition.y;
            mousePosition.x += didimoThumbnailsScrollPosition.x;

            for (int i = 0; i < thumbnailRects.Count; i++)
            {
                Rect itemRect = new Rect(thumbnailRects[i]);
                //itemRect.height -= 75;
                if (itemRect.Contains(mousePosition))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Draw the Didimo thumbnails.
        /// </summary>
        /// <param name="viewRect">The Rect where to draw the Didimo scrollview.</param>
        public void DrawDidimoThumbnails(Rect viewRect)
        {
            this.viewRect = new Rect(viewRect.x, viewRect.y, viewRect.width, viewRect.height-75);
            visibleRect = viewRect;
            visibleRect.y = didimoThumbnailsScrollPosition.y;
            visibleRect.x = didimoThumbnailsScrollPosition.x;

            float newThumbnailSize = viewRect.width - GUI.skin.verticalScrollbar.fixedWidth;

            if (newThumbnailSize != thumbnailSize)
            {
                thumbnailSize = newThumbnailSize;
                thumbnailRects.Clear();
                GenerateDidimoThumbnailRects();
            }

            if (Event.current.mousePosition.y < GetVisibleRectHeight()) //to prevent the event overlapping the button area
                HandleEvent(Event.current);

            Rect scrollviewContentRect = new Rect(0, 0, thumbnailSize, (thumbnailSize + 1) * thumbnailRects.Count);
            didimoThumbnailsScrollPosition = GUI.BeginScrollView(viewRect, didimoThumbnailsScrollPosition, scrollviewContentRect, false, true);

            for (int i = 0; i < didimos.Count; i++)
            {
                if (IsDidimoVisible(i))
                {
                    DrawListItem(i, didimos[i]);
                }
            }
            GUI.EndScrollView();

            GUILayout.FlexibleSpace();

        }


        /// <summary>
        /// From the selected Didimos, get those that are available to be imported.
        /// </summary>
        /// <returns>A list of Didimos, from the selected Didimos, that are available to be imported.</returns>
        public List<DidimoStatusDataObject> SelectedDidimosAvailableToImport()
        {
            List<DidimoStatusDataObject> didimosToDownload = new List<DidimoStatusDataObject>();

            foreach (int i in selectedDidimoIndices)
            {
                if (didimos[i].didimoModel.isDone && !didimos[i].didimoModel.hasFailed)
                {
                    didimosToDownload.Add(didimos[i].didimoModel);
                }
            }

            return didimosToDownload;
        }

        void UpdateDidimoThumbnailsIfNeeded()
        {

            List<DidimoThumbnail> didimosToDownloadImages = new List<DidimoThumbnail>();

            foreach (DidimoThumbnail didimo in didimos)
            {
                if (didimo.thumbnail == null)
                {
                    didimosToDownloadImages.Add(didimo);
                }
            }

            coroutineManager.StartCoroutine(DownloadImagesForDidimos(didimosToDownloadImages));
        }

        void StopAllCreationProgressUpdates()
        {
            if (didimos != null)
            {
                foreach (DidimoThumbnail didimo in didimos)
                {
                    didimo.StopCreationProcessUpdate();
                }
            }
        }

        IEnumerator DownloadImagesForDidimos(List<DidimoThumbnail> didimosToDownloadImages)
        {
            while (didimosToDownloadImages.Count > 0)
            {
                DidimoThumbnail didimo = didimosToDownloadImages[0];

                if (didimo.didimoModel.isDone)
                { 
                    yield return ServicesRequests.EditorInstance.DownloadDidimoThumbnail(
                         coroutineManager,
                         didimo.didimoModel.key,
                         texture =>
                         {
                             didimo.thumbnail = texture;
                             didimo.thumbnail.hideFlags = HideFlags.HideAndDontSave;
                             if (IsDidimoVisible(didimos.IndexOf(didimo)))
                             {
                                 if (repaintDelegate != null)
                                 {
                                     repaintDelegate();
                                 }
                             }
                         },
                         exception => { });
                }

                didimosToDownloadImages.Remove(didimo);
            }
        }

        bool IsDidimoVisible(int itemIndex)
        {
            if (itemIndex > -1 && itemIndex < thumbnailRects.Count)
                return thumbnailRects[itemIndex].Overlaps(visibleRect);
            else return false;
        }

        /// <summary>
        /// Hack to repeat an event. Used when we need to repaint before performing an action.
        /// </summary>
        /// <param name="repeatEvent">The event to repeat.</param>
        /// <returns>An IEnumerator</returns>
        IEnumerator DelayedRepeatEvent(Event repeatEvent)
        {
            yield return new EditorWaitForSeconds(0);
            lastEventMousePosition = repeatEvent.mousePosition;
            sendEventDelegate(repeatEvent);
        }

        void HandleEvent(Event eventToHandle)
        {
            if (eventToHandle.type != EventType.MouseDown && eventToHandle.type != EventType.ContextClick)
            {
                return;
            }

            //For some reason, when the even from 'Monobehaviour.SendEvent' is handled, the mouse position is wrong. Lets restore it.
            if (lastEventMousePosition.HasValue)
            {
                Event.current.mousePosition = lastEventMousePosition.Value;
                lastEventMousePosition = null;
            }

            int hoveredItemIndex = GetHoveredItemIndex(eventToHandle);

            if (hoveredItemIndex != -1)
            {
                if (eventToHandle.type == EventType.ContextClick)
                {
                    if (!selectedDidimoIndices.Contains(hoveredItemIndex))
                    {
                        DeselectAllItems();
                        lastSelectedItemIndex = hoveredItemIndex;
                        selectedDidimoIndices.Add(lastSelectedItemIndex);
                        didSelectDidimoDelegate(didimos[hoveredItemIndex].didimoModel);
                        repaintDelegate();
                        coroutineManager.StartCoroutine(DelayedRepeatEvent(new Event(eventToHandle)));
                        eventToHandle.Use();
                    }
                    else
                    {
                        ShowRightClickMenu();
                        eventToHandle.Use();
                    }
                }
                else
                {
                    if (eventToHandle.type == EventType.MouseDown && eventToHandle.button == 0)
                    {
                        if (lastSelectedItemIndex != -1 && eventToHandle.control)
                        {
                            if (selectedDidimoIndices.Contains(hoveredItemIndex))
                            {
                                selectedDidimoIndices.Remove(hoveredItemIndex);
                                if (selectedDidimoIndices.Count > 0)
                                {
                                    lastSelectedItemIndex = selectedDidimoIndices[selectedDidimoIndices.Count - 1];
                                }
                                else
                                {
                                    lastSelectedItemIndex = -1;
                                }
                            }
                            else
                            {
                                selectedDidimoIndices.Add(hoveredItemIndex);
                                lastSelectedItemIndex = hoveredItemIndex;
                            }
                        }
                        else if (lastSelectedItemIndex != -1 && eventToHandle.shift)
                        {
                            SelectItemsBetweenRange(lastSelectedItemIndex, hoveredItemIndex);
                        }
                        else
                        {
                            DeselectAllItems();
                            lastSelectedItemIndex = hoveredItemIndex;
                            selectedDidimoIndices.Add(lastSelectedItemIndex);
                        }
                        eventToHandle.Use();
                    }

                    didSelectDidimoDelegate(lastSelectedItemIndex == -1 ? null : didimos[lastSelectedItemIndex].didimoModel);
                }
            }
        }

        void ShowRightClickMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Preview"), false, () =>
                {
                    DeselectAllItems();
                    selectedDidimoIndices.Add(lastSelectedItemIndex);
                });
            menu.AddItem(new GUIContent("Show metadata"), false, () =>
            {
                string msg = "";
                msg += "Didimo Key: " + didimos[lastSelectedItemIndex].didimoModel.key;
                msg += "\nCreation Date: " + didimos[lastSelectedItemIndex].didimoModel.updated_at;

                if (didimos[lastSelectedItemIndex].didimoModel.meta.Count > 0)
                {
                    msg += "\n----------------------------------------";
                    foreach (DidimoMetadataDataObject meta in didimos[lastSelectedItemIndex].didimoModel.meta)
                    {
                        msg += "\n" + meta.name + ": " + meta.value;
                    }
                }
                
                if (EditorUtility.DisplayDialog("Didimo Key",
                  msg,
                       "Ok"))
                { }
            });

            bool isImportAvailable = didimos[lastSelectedItemIndex].didimoModel != null && didimos[lastSelectedItemIndex].didimoModel.hasUnityTarget;
            if(isImportAvailable)
                menu.AddItem(new GUIContent("Import"), false, () =>
                {
                    buyAndImportSelectedItemsDelegate(selectedDidimoIndices);
                });
            else menu.AddDisabledItem(new GUIContent("Import"));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Didimo Deletion",
                   "This action cannot be undone. Do you wish to continue?",
                        "Yes",
                        "No"))
                {
                    coroutineManager.StartCoroutine(DeleteSelectedDidimos(didimos));
                }
            });
            menu.ShowAsContext();
        }

        IEnumerator DeleteSelectedDidimos(List<DidimoThumbnail> didimos)
        {
            List<DidimoThumbnail> didimosToDelete = new List<DidimoThumbnail>();
            foreach (int i in selectedDidimoIndices)
            {
                didimosToDelete.Add(didimos[i]);
            }
            lastSelectedItemIndex = -1;
            float totalDidimosToDelete = didimosToDelete.Count;

            while (didimosToDelete.Count > 0)
            {
                EditorUtility.DisplayProgressBar("Deleting Didimos", "Deleting selected Didimos, please wait", (totalDidimosToDelete - didimosToDelete.Count) / totalDidimosToDelete);

                DidimoThumbnail didimo = didimosToDelete[0];
                bool error = false;

                yield return ServicesRequests.EditorInstance.DeleteDidimo(
                    new EditorCoroutineManager(),
                    didimo.didimoModel.key,
                    () =>
                    {
                        didimos.Remove(didimo);
                        selectedDidimoIndices.RemoveAt(0);
                        didimosToDelete.RemoveAt(0);
                    },
                    exception =>
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Failed to delete didimo", exception.Message, "OK");
                    });

                if (error)
                {
                    didimosToDelete.Clear();
                }
            }
            didSelectDidimoDelegate(null);
            thumbnailRects.Clear();
            GenerateDidimoThumbnailRects();
            EditorUtility.ClearProgressBar();
            repaintDelegate();
        }

        void SelectItemsBetweenRange(int from, int to)
        {
            selectedDidimoIndices.Clear();
            int max = Mathf.Max(from, to);
            int min = Mathf.Min(from, to);

            for (int i = 0; i < thumbnailRects.Count; i++)
            {
                bool selected = i >= min && i <= max;

                if (selected)
                {
                    selectedDidimoIndices.Add(i);
                }
            }
        }

        void DeselectAllItems()
        {
            selectedDidimoIndices.Clear();
        }

        void GenerateDidimoThumbnailRects()
        {
            if (thumbnailRects.Count > didimos.Count)
            {
                thumbnailRects.RemoveRange(didimos.Count, thumbnailRects.Count - didimos.Count);
            }
            else
            {
                for (int i = thumbnailRects.Count; i < didimos.Count; i++)
                {
                    thumbnailRects.Add(new Rect(thumbnailMargins, (thumbnailSize + thumbnailMargins) * i, thumbnailSize - thumbnailMargins, thumbnailSize));
                }
            }
        }

        void DrawListItem(int index, DidimoThumbnail didimo)
        {
            Rect rect = thumbnailRects[index];
            bool selected = selectedDidimoIndices.Contains(index);

            Rect thumbnailRect = rect;
            if (selected)
            {
                GUI.DrawTexture(rect, listItemSelectedTexture, ScaleMode.ScaleAndCrop);
                Rect selectedRect = new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6);
                thumbnailRect = selectedRect;
            }

            GUI.DrawTexture(thumbnailRect, didimo.thumbnail != null ? didimo.thumbnail : thumbnailPlaceholderTexture, ScaleMode.ScaleAndCrop);

            Rect lockIconRect = new Rect(rect.x + lockIconMargins.x,
                rect.y + lockIconMargins.y,
                rect.width - lockIconMargins.x,
                rect.height - lockIconMargins.y);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.font = iconFont;
            labelStyle.fontSize = lockiconFontSize;
            labelStyle.alignment = TextAnchor.UpperLeft;
            if (didimo.didimoModel.hasFailed)
            {
                labelStyle.normal.textColor = Color.white;
                char lockChar = '\xe061';
                GUI.Label(lockIconRect, new GUIContent(lockChar.ToString()), labelStyle);

                labelStyle.normal.textColor = Color.black;
                lockChar = '\x72';
                GUI.Label(lockIconRect, new GUIContent(lockChar.ToString()), labelStyle);
            }
            // Ignore this for now, as Didimos are paid for on creation
            //else if (!didimo.didimoModel.paid)
            //{

            //    labelStyle.normal.textColor = Color.white;
            //    char errorChar = '\xe06c';
            //    GUI.Label(lockIconRect, new GUIContent(errorChar.ToString()), labelStyle);

            //    errorChar = '\x7e';
            //    labelStyle.normal.textColor = Color.black;
            //    GUI.Label(lockIconRect, new GUIContent(errorChar.ToString()), labelStyle);
            //}

            if (!didimo.didimoModel.isDone && didimo.didimoModel.percent != 100.0f)
            {
                Rect progressRect = new Rect(rect);
                progressRect.y += (progressRect.height - 7);
                progressRect.height = 7;
                //EditorGUI.ProgressBar(progressRect, didimo.didimoModel.percent / 100f, "");
                //EditorGUI.ProgressBar(progressRect, didimo.didimoModel.percent / 100f, "");

                int progress_bar_size = (int) (progressRect.width * progressRect.height);
                UnityEngine.Color[] bg_color = new Color[progress_bar_size];
                UnityEngine.Color[] fg_color = new Color[progress_bar_size];
                for (var i = 0; i < progress_bar_size; ++i)
                {
                    bg_color[i] = Color.black;
                    fg_color[i] = Color.green;
                }
                Texture2D progressBackgroundTexture = new Texture2D((int)progressRect.width, (int)progressRect.height);
                progressBackgroundTexture.SetPixels(bg_color, 0);
                progressBackgroundTexture.Apply();
                Texture2D progressForgroundTexture = new Texture2D((int)progressRect.width, (int)progressRect.height);
                progressForgroundTexture.SetPixels(fg_color, 0);
                progressForgroundTexture.Apply();
                GUI.DrawTexture(progressRect, progressBackgroundTexture);
                GUI.DrawTexture(new Rect(progressRect.x, progressRect.y, progressRect.width * (didimo.didimoModel.percent / 100f), progressRect.height), progressForgroundTexture);
            }
        }

        [System.Serializable]
        public class DidimoThumbnail
        {
            public DidimoStatusDataObject didimoModel;
            public Texture2D thumbnail;
            private bool checkingCreationProgress;
            private CoroutineManager coroutineManager;

            public DidimoThumbnail(DidimoStatusDataObject model)
            {
                didimoModel = model;
            }

            ~DidimoThumbnail()
            {
                if(coroutineManager != null) { 
                    coroutineManager.StopAllCoroutines();
                }
            }

            public void StopCreationProcessUpdate()
            {
                if (coroutineManager != null)
                {
                    coroutineManager.StopAllCoroutines();
                }
                checkingCreationProgress = false;
                coroutineManager = null;
            }

            public void UpdateCreationProcessIfNeeded(System.Action progressUpdateAction)
            {
                if (checkingCreationProgress || didimoModel.isDone)
                {
                    return;
                }

                StopCreationProcessUpdate();

                if (coroutineManager == null)
                { 
                    coroutineManager = new EditorCoroutineManager();
                }
                checkingCreationProgress = true;
                ServicesRequests.EditorInstance.DidimoCreationProgress(
                    coroutineManager,
                    didimoModel.key,
                    () =>
                    {
                        checkingCreationProgress = false;
                        didimoModel.status = "done";
                        didimoModel.percent = 100;
                        progressUpdateAction();
                    },
                    progress =>
                    {
                        if (didimoModel.percent != progress)
                        {
                            didimoModel.percent = (int)progress;
                            progressUpdateAction();
                        }
                    },
                    exception =>
                    {
                        checkingCreationProgress = false;
                        Debug.LogError("Failed to check the progress of Didimo creation. Please hit refresh to start syncing again.");
                    });
            }
        }
    }
}