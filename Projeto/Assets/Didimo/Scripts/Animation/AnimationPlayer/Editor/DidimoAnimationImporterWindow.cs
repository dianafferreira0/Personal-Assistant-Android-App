using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Didimo.Animation.AnimationPlayer.Editor
{
    public class DidimoAnimationImporterWindow : EditorWindow
    {
        const string PLAY_BUTTON_STR = "I";
        const string PAUSE_BUTTON_STR = "_";
        const string PLUS_BUTTON_STR = "L";
        const string MINUS_BUTTON_STR = "M";
        const int DEFAULT_FPS = 60;

        [Serializable]
        private class AnimationConfig
        {
            public string startFrameStr;
            public string endFrameStr;
            public int startFrame;
            public int endFrame;
            public string animationName;
            public WrapMode wrapMode;
        }

        PreviewRenderUtility prevRenderer = null;
        [SerializeField]
        GameObject prefab = null;
        [SerializeField]
        GameObject prefabClone = null;
        [SerializeField]
        AnimationPlayer animationPlayer = null;
        [SerializeField]
        DidimoAnimation fullAnimation = null;
        [SerializeField]
        List<AnimationConfig> animations = null;
        [SerializeField]
        int previewAnimationIndex = -1;

        [SerializeField]
        bool interpolateBetweenFrames = true;

        bool shouldRepaint = false;

        [MenuItem("Window/Didimo/Animation Importer")]
        public static void ShowWindow()
        {
            DidimoAnimationImporterWindow window = (DidimoAnimationImporterWindow)EditorWindow.GetWindow(typeof(DidimoAnimationImporterWindow));
            window.minSize = new Vector2(300, 250);
            window.titleContent = new GUIContent("Didimo Animation Importer");
        }

        /// <summary>
        /// Get all meshes, and transforms and materials of those meshes
        /// </summary>
        /// <param name="meshes">Output of the list of meshes</param>
        /// <param name="transforms">Output list of the localToWorld transform of each mesh</param>
        /// <param name="materials">Output list of the list of materials per mesh</param>
        void SampleAllMeshes(out List<Mesh> meshes, out List<Matrix4x4> localToWorldMatrices, out List<List<Material>> materials)
        {
            meshes = new List<Mesh>();
            localToWorldMatrices = new List<Matrix4x4>();
            materials = new List<List<Material>>();

            SkinnedMeshRenderer[] renderers = prefabClone.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sharedMesh)
                {
                    renderers[i].enabled = false;// Don't draw on the scene
                    renderers[i].updateWhenOffscreen = true;
                    renderers[i].forceMatrixRecalculationPerRender = true;
                    Mesh mesh = new Mesh();
                    renderers[i].BakeMesh(mesh);
                    meshes.Add(mesh);
                    localToWorldMatrices.Add(renderers[i].localToWorldMatrix);

                    List<Material> materialsOfMesh = new List<Material>();
                    for (int j = 0; j < mesh.subMeshCount; j++)
                    {
                        materialsOfMesh.Add(renderers[i].sharedMaterials[j]);
                    }
                    materials.Add(materialsOfMesh);
                }
            }

            MeshFilter[] meshFilters = prefabClone.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh)
                {
                    MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer>();
                    meshRenderer.enabled = false; // Don't draw on the scene
                    meshes.Add(meshFilters[i].sharedMesh);
                    localToWorldMatrices.Add(meshFilters[i].transform.localToWorldMatrix);

                    List<Material> materialsOfMesh = new List<Material>();
                    for (int j = 0; j < meshFilters[i].sharedMesh.subMeshCount; j++)
                    {
                        if (meshRenderer != null)
                        {
                            materialsOfMesh.Add(meshRenderer.sharedMaterials[j]);
                        }
                    }
                    materials.Add(materialsOfMesh);
                }
            }
        }

        GenericMenu dropDownMenu = null;
        void UpdatePreviewDropDownEntries()
        {
            dropDownMenu = new GenericMenu();

            for (int i = 0; i < animations.Count; i++)
            {
                dropDownMenu.AddItem(new GUIContent(animations[i].animationName), i == previewAnimationIndex, (obj) =>
                {
                    int index = (int)obj;
                    if (index != previewAnimationIndex)
                    {
                        AddAnimationForPreview(index);
                    }
                }, i);
            }
        }

        void FocusCameraOnMeshes(List<Mesh> meshes, List<Matrix4x4> localToWorldMatrices)
        {
            Bounds combinedWorldSpaceBounds = new Bounds();
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].RecalculateBounds();
                Vector3 max = localToWorldMatrices[i].MultiplyPoint(meshes[i].bounds.max);
                Vector3 min = localToWorldMatrices[i].MultiplyPoint(meshes[i].bounds.min);
                Vector3 size = max - min;

                Bounds meshBounds = new Bounds(localToWorldMatrices[i].MultiplyPoint(meshes[i].bounds.center), size);
                if (i == 0)
                {
                    combinedWorldSpaceBounds = meshBounds;
                }
                else
                {
                    combinedWorldSpaceBounds.Encapsulate(meshBounds);
                }
            }

            float frustumHeight = combinedWorldSpaceBounds.size.y;
            var distance = frustumHeight * 0.5f / Mathf.Tan(prevRenderer.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            distance += combinedWorldSpaceBounds.extents.z;

            Vector3 cameraPos = Vector3.zero;
            cameraPos.x = combinedWorldSpaceBounds.center.x;
            cameraPos.y = combinedWorldSpaceBounds.center.y;
            cameraPos.z = distance;

            prevRenderer.camera.transform.position = cameraPos;

            prevRenderer.camera.transform.LookAt(combinedWorldSpaceBounds.center, Vector3.up);
        }

        void BeginDraw(Rect r)
        {
            if (prevRenderer == null)
            {
                prevRenderer = new PreviewRenderUtility();
            }

            prevRenderer.camera.farClipPlane = 300f;
            prevRenderer.camera.nearClipPlane = 0.003f;
            prevRenderer.camera.fieldOfView = 60;

            prevRenderer.lights[0].intensity = 1f;
            prevRenderer.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0f);
            prevRenderer.lights[1].intensity = 2f;

            prevRenderer.BeginPreview(r, GUIStyle.none);
        }

        void EndDraw(Rect r)
        {
            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            prevRenderer.camera.Render();
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);

            Texture texture = prevRenderer.EndPreview();
            GUI.DrawTexture(r, texture);
        }

        public void DrawRenderPreview(List<Mesh> meshes, List<Matrix4x4> localToWorldMatrices, List<List<Material>> materials)
        {

            for (int i = 0; i < meshes.Count; i++)
            {
                for (int j = 0; j < materials[i].Count; j++)
                {
                    prevRenderer.DrawMesh(meshes[i], localToWorldMatrices[i], materials[i][j], j);
                }
            }
        }

        void ClonePrefab()
        {
            if (prefabClone)
            {
                DestroyImmediate(prefabClone);
            }

            prefabClone = Instantiate(prefab);
            prefabClone.hideFlags = HideFlags.HideAndDontSave;
            prefabClone.SetActive(false);

            animationPlayer = prefabClone.GetComponentInChildren<AnimationPlayer>();
            if (animationPlayer == null)
            {
                prefab = null;
                DestroyImmediate(prefabClone);
                prefabClone = null;
                Debug.LogWarning("Prefab must have the AnimationPlayer component.");
            }
            else
            {
                animationPlayer.RemoveAllAnimations();
                animationPlayer.realtimeRig.Init();
            }

            if (fullAnimation != null)
            {
                if (previewAnimationIndex == -1)
                {
                    previewAnimationIndex = animations.Count - 1;
                }
                AddAnimationForPreview(previewAnimationIndex);
            }

            shouldRepaint = true;
        }

        List<List<float>> GetFacValuesForAnimation(AnimationConfig animation)
        {
            List<List<float>> result = new List<List<float>>();

            for (int i = 0; i < fullAnimation.facNames.Count; i++)
            {
                List<float> values = new List<float>();
                for (int j = animation.startFrame; j <= animation.endFrame; j++)
                {
                    values.Add(fullAnimation.facValues[i][j]);
                }
                result.Add(values);
            }

            return result;
        }

        void AddAnimationForPreview(int animationIndex)
        {
            if (animationPlayer != null)
            {
                float normalizedTime = 0;
                // If we didn't change animation (e.g. we are updating the animation we were already previewing), we will keep the same normalized time
                if (previewAnimationIndex == animationIndex && animationPlayer.GetAnimationCount() != 0)
                {
                    normalizedTime = animationPlayer.GetAnimation(0).NormalizedTime;
                }
                else
                {
                    animationPlayer.PauseAnimations(true);
                }

                previewAnimationIndex = animationIndex;

                animationPlayer.RemoveAllAnimations();

                DidimoAnimation animation = DidimoAnimation.CreateInstanceForConfig(
                    animations[previewAnimationIndex].animationName,
                    fullAnimation.facNames,
                    GetFacValuesForAnimation(animations[previewAnimationIndex]),
                    DEFAULT_FPS,
                    animations[previewAnimationIndex].wrapMode);

                animationPlayer.AddAnimationTrack(animation);
                animation.Play();
                animation.NormalizedTime = normalizedTime;
            }
        }


        Vector2 scrollposition = new Vector2();
        Rect lastRect = new Rect();
        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Not available during play.", MessageType.Info);
                return;
            }

            if (prefabClone == null && prefab != null)
            {
                ClonePrefab();
            }

            if (prefabClone != null)
            {
                prefabClone.SetActive(false);
            }

            Rect r = new Rect(250f, 0.0f, position.width - 250f, position.height);

            BeginDraw(r);

            if (prefabClone != null)
            {
                List<Mesh> meshes;
                List<Matrix4x4> localToWorldMatrices;
                List<List<Material>> materials;

                SampleAllMeshes(out meshes, out localToWorldMatrices, out materials);

                FocusCameraOnMeshes(meshes, localToWorldMatrices);

                DrawRenderPreview(meshes, localToWorldMatrices, materials);

            }
            EndDraw(r);

            GUILayout.Label("Preview with Didimo:");
            GameObject oldPrefab = prefab;
            prefab = (GameObject)EditorGUILayout.ObjectField("", prefab, typeof(GameObject), true, GUILayout.Width(245));
            if (prefab != null)
            {
                if (oldPrefab != prefab)
                {
                    ClonePrefab();
                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxWidth(245));
            }

            if (GUILayout.Button("Load Animation File", GUILayout.MaxWidth(245)))
            {
                LoadAnimation();
            }

            DrawAnimationsList();

            DrawAnimationPreviewControls();

            if (shouldRepaint && Event.current.type == EventType.Repaint)
            {
                Repaint();
                shouldRepaint = false;
            }
        }

        void DrawAnimationsList()
        {
            const float HEIGHT = 80;
            const float VERTICAL_SPACING = 5;
            const float HORIZONTAL_SPACING = 5;
            const float WIDTH = 240;
            const float SCROLL_BAR_WIDTH = 15;
            const float ENTRY_WIDTH = 245;

            if (animations != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Animation", GUILayout.Width(210));
                if (GUILayout.Button(PLUS_BUTTON_STR, GetElegantIconsButtonStyle(), GUILayout.Width(30)))
                {
                    AddAnimationConfig();
                }
                GUILayout.EndHorizontal();

                if (Event.current.type == EventType.Repaint)
                {
                    lastRect = GUILayoutUtility.GetLastRect();
                }

                GUILayout.BeginArea(new Rect(0, lastRect.min.y + lastRect.height, ENTRY_WIDTH, position.height - (lastRect.min.y + lastRect.height) - 30));

                scrollposition = GUILayout.BeginScrollView(scrollposition, false, true);

                float startFrame;
                float endFrame;
                Rect rect = new Rect(HORIZONTAL_SPACING, VERTICAL_SPACING, WIDTH - SCROLL_BAR_WIDTH - HORIZONTAL_SPACING, HEIGHT);
                for (int i = 0; i < animations.Count; i++)
                {
                    AnimationConfig animationConfig = animations[i];
                    GUILayout.BeginArea(rect, EditorStyles.helpBox);

                    GUILayout.BeginHorizontal();
                    animationConfig.animationName = GUILayout.TextField(animationConfig.animationName);
                    if (GUILayout.Button(MINUS_BUTTON_STR, GetElegantIconsButtonStyle(), GUILayout.Width(30)))
                    {
                        if (previewAnimationIndex <= i)
                        {
                            previewAnimationIndex--;
                        }

                        animations.RemoveAt(i);
                        i--;
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    startFrame = animationConfig.startFrame;
                    endFrame = animationConfig.endFrame;

                    EditorGUILayout.MinMaxSlider(ref startFrame, ref endFrame, 0, fullAnimation.GetNumberOfFrames() - 1);

                    if (startFrame != animationConfig.startFrame || endFrame != animationConfig.endFrame)
                    {
                        animationConfig.startFrame = (int)startFrame;
                        animationConfig.endFrame = (int)endFrame;
                        animationConfig.startFrameStr = animationConfig.startFrame.ToString();
                        animationConfig.endFrameStr = animationConfig.endFrame.ToString();
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Start:");
                    animationConfig.startFrameStr = GUILayout.TextField(animationConfig.startFrameStr);
                    GUILayout.Label("End:");
                    animationConfig.endFrameStr = GUILayout.TextField(animationConfig.endFrameStr);
                    GUILayout.EndHorizontal();

                    int value = 0;
                    if (int.TryParse(animationConfig.startFrameStr, out value))
                    {
                        animationConfig.startFrame = value;
                    }
                    if (int.TryParse(animationConfig.endFrameStr, out value))
                    {
                        animationConfig.endFrame = value;
                    }

                    animationConfig.wrapMode = (WrapMode)EditorGUILayout.EnumPopup("Wrap mode", animationConfig.wrapMode);

                    GUILayout.EndArea();
                    rect.y += HEIGHT + VERTICAL_SPACING;
                }

                // BeginArea doesn't fill scrollviews, so we need to use this hack to set the proper size to the scrollview's content
                GUILayout.Label("", GUILayout.Height(rect.y));

                GUILayout.EndScrollView();

                GUILayout.EndArea();

                GUILayout.BeginArea(new Rect(HORIZONTAL_SPACING, position.height - 25, ENTRY_WIDTH - HORIZONTAL_SPACING, 30));
                if (GUILayout.Button("Save All"))
                {
                    //string path = EditorUtility.OpenFolderPanel("Select a folder", "", "");
                    string path = EditorUtility.SaveFolderPanel("Select a folder", "", "");

                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!path.StartsWith(Application.dataPath))
                        {
                            EditorUtility.DisplayDialog("Bad path", "Must select a folder within the Assets folder of this project.", "OK");
                        }
                        else
                        {
                            foreach (AnimationConfig animation in animations)
                            {
                                DidimoAnimation didimoAnimation =
                                    DidimoAnimation.CreateInstanceForConfig(
                                                                    animation.animationName,
                                                                    fullAnimation.facNames,
                                                                    GetFacValuesForAnimation(animation),
                                                                    DEFAULT_FPS,
                                                                    animation.wrapMode);


                                string assetPath = "Assets/" + path.Replace(Application.dataPath, "");
                                assetPath = Path.Combine(assetPath, animation.animationName + ".asset");
                                DidimoAnimation previousDidimoAnimation = null;
                                if (previousDidimoAnimation = AssetDatabase.LoadAssetAtPath<DidimoAnimation>(assetPath))
                                {
                                    EditorUtility.CopySerialized(didimoAnimation, previousDidimoAnimation);
                                    AssetDatabase.SaveAssets();
                                }
                                else
                                {
                                    AssetDatabase.CreateAsset(didimoAnimation, assetPath);
                                }

                            }
                        }
                    }
                }
                GUILayout.EndArea();
            }
        }

        float previousTime = 0;
        void Update()
        {

            if (Application.isPlaying)
            {
                return;
            }

            if (animationPlayer != null && previewAnimationIndex != -1 && fullAnimation != null)
            {
                if (animationPlayer.GetAnimationCount() == 0)
                {
                    AddAnimationForPreview(previewAnimationIndex);
                }
                if (animationPlayer.IsPlaying || previousTime != animationPlayer.GetAnimation(0).NormalizedTime)
                {
                    previousTime = animationPlayer.GetAnimation(0).NormalizedTime;
                    prefabClone.SetActive(true);
                    if (animationPlayer.IsPlaying)
                    {
                        animationPlayer.Update();
                    }
                    else
                    {
                        animationPlayer.GetAnimation(0).Play();
                        animationPlayer.GetAnimation(0).NormalizedTime = previousTime;
                        animationPlayer.UpdatePose();
                    }
                    animationPlayer.realtimeRig.UpdateRigDeformation();
                    Repaint();

                    if (animationPlayer.GetAnimation(0).IsStopped)
                    {
                        animationPlayer.PauseAnimations(true);
                    }
                }
            }

        }

        //private void OnInspectorUpdate()
        //{
        //    if (shouldRepaint)
        //    {
        //        Repaint();
        //        shouldRepaint = false;
        //    }
        //}

        Font _font = null;
        Font GetElegantFont()
        {
            if (_font == null)
            {
                _font = Resources.Load<Font>("ElegantIcons");
            }

            return _font;
        }

        static GUIStyle _playButtonStyle = null;
        static Texture2D _emptyTexture = null;
        GUIStyle GetElegantIconsButtonStyle()
        {
            if (_playButtonStyle == null || _emptyTexture == null)
            {
                Texture2D texture = new Texture2D(0, 0);
                _playButtonStyle = new GUIStyle();
                _playButtonStyle.font = GetElegantFont();
                _playButtonStyle.fontSize = 21;
                _playButtonStyle.alignment = TextAnchor.MiddleCenter;
                _playButtonStyle.normal.textColor = new Color(1, 1, 1);
                _playButtonStyle.normal.background = texture;
                _playButtonStyle.onHover.textColor = new Color(1, 1, 1);
                _playButtonStyle.onHover.background = texture;
                _playButtonStyle.hover.textColor = new Color(1, 1, 1);
                _playButtonStyle.hover.background = texture;
                _playButtonStyle.onActive.textColor = new Color(0.4f, 0.4f, 0.4f);
                _playButtonStyle.onActive.background = texture;
                _playButtonStyle.active.textColor = new Color(0.4f, 0.4f, 0.4f);
                _playButtonStyle.active.background = texture;
                _playButtonStyle.focused.textColor = new Color(1, 1, 1);
                _playButtonStyle.focused.background = texture;
                _playButtonStyle.onFocused.textColor = new Color(1, 1, 1);
                _playButtonStyle.onFocused.background = texture;
                _playButtonStyle.onActive.textColor = new Color(0f, .8f, .8f);
                _playButtonStyle.onActive.background = texture;
            }
            return _playButtonStyle;
        }

        void DrawAnimationPreviewControls()
        {
            if (previewAnimationIndex != -1 && animations != null && prefabClone != null && fullAnimation != null)
            {
                GUILayout.BeginArea(new Rect(260, position.height - 90, position.width - 265, 80));

                UpdatePreviewDropDownEntries();
                if (animations.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Preview animation");
                    if (GUILayout.Button(animations[previewAnimationIndex].animationName))
                    {
                        dropDownMenu.ShowAsContext();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                }

                interpolateBetweenFrames = GUILayout.Toggle(interpolateBetweenFrames, "Interpolate between frames");
                animationPlayer.interpolateBetweenFrames = interpolateBetweenFrames;

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);

                string buttonStr = animationPlayer.IsPlaying ? PAUSE_BUTTON_STR : PLAY_BUTTON_STR;
                if (GUILayout.Button(buttonStr, GetElegantIconsButtonStyle(), GUILayout.Width(31), GUILayout.Height(30)))
                {
                    animationPlayer.PauseAnimations(animationPlayer.IsPlaying);
                    AddAnimationForPreview(previewAnimationIndex);
                }

                GUILayout.Space(10);
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                DidimoAnimation animation = animationPlayer.GetAnimation(0);
                animation.NormalizedTime = GUILayout.HorizontalSlider(animation.NormalizedTime, 0, 1);
                GUILayout.EndVertical();
                GUILayout.Space(10);
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }
        }

        void LoadAnimation()
        {
            string path = EditorUtility.OpenFilePanel("Select an animation file", "", "json");

            if (!string.IsNullOrEmpty(path))
            {
                StreamReader reader = new StreamReader(path);
                fullAnimation = DidimoAnimation.CreateInstanceFromJsonString(Path.GetFileName(path), reader.ReadToEnd());

                animations = new List<AnimationConfig>();
                AddAnimationConfig();
                fullAnimation.wrapMode = WrapMode.Once;
                shouldRepaint = true;
            }
        }

        void AddAnimationConfig()
        {
            AnimationConfig animationConfig = new AnimationConfig();
            animationConfig.animationName = string.Format("Take{0:D3}", animations.Count + 1);
            animationConfig.startFrame = 0;
            animationConfig.endFrame = fullAnimation.GetNumberOfFrames() - 1;
            animationConfig.startFrameStr = animationConfig.startFrame.ToString();
            animationConfig.endFrameStr = animationConfig.endFrame.ToString();
            animationConfig.wrapMode = WrapMode.Once;
            animations.Add(animationConfig);

            AddAnimationForPreview(animations.Count - 1);
        }

        private void OnDisable()
        {
            if (prevRenderer != null)
            {
                prevRenderer.Cleanup();
            }

            if (prefabClone != null)
            {
                DestroyImmediate(prefabClone);
            }
        }
    }
}
