using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Didimo.Animation;
using Didimo.Animation.AnimationPlayer;
using Didimo.DidimoManagement;
using Didimo.Menu.Scrollers;
using Didimo.Networking.DataObjects;
using Didimo.Speech;
using Didimo.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    public class DidimoMenuHandler : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField]
        RectTransform loadingMenu = null;
        [SerializeField]
        RectTransform errorMenu = null;
        [SerializeField]
        Text errorMenuText = null;
        [SerializeField]
        RectTransform backArrow = null;
        [SerializeField]
        Text progressText = null;
        [SerializeField]
        GameObject pauseMenu = null;
        [SerializeField]
        Toggle pauseToggle = null;
        [SerializeField]
        Button backButton = null;
        [SerializeField]
        HairConfig hairConfig = null;
        [SerializeField]
        GameObject hairConfigPanel = null;
        [SerializeField]
        GameObject animButtonsPanel = null;

        [SerializeField] PlayerTogglesHandler togglesHandler = null;

        [SerializeField] EyesGalleryHorizontalScroller eyesScroller = null;
        [SerializeField] HairColorGalleryHorizontalScroller_v2 hairColorScroller = null;
        [SerializeField] MocapGalleryHorizontalScroller mocapScroller = null;

        [SerializeField] AnimationManager animManager = null;
        [SerializeField] GameObject RigAndAnimationMismatchWarningLabel = null;
        [SerializeField] VisemePlayer speechManager = null;

        private bool shouldDisplayHairScroller = false;
        System.Action onCancelDelegate;
        public string defaultLoadingText = "Loading...";
        public GameObject LoadingPanel = null;
        Text loadingText = null;

        [SerializeField]
        DidimoImporter didimoImporter = null;
        public DidimoImporter DidimoImporter { get => didimoImporter; }

        bool hasDidimoBeenLoaded = false; //to display the actions panels when hiding the main menu
        #endregion

        void Awake()
        {
            loadingText = loadingMenu.FindRecursive("Text").GetComponent<Text>();
        }

        #region MENU

        public void GoBack()
        {
            backButton.onClick.Invoke();
        }

        public void HideMenu()
        {
            progressText.text = "";
            LoadingPanel.SetActive(false);

            pauseMenu.SetActive(false);

            if (hasDidimoBeenLoaded)
                animButtonsPanel.SetActive(true);
        }

        public void ToggleMenu(Toggle t)
        {
            if (t.isOn)
            {
                pauseMenu.SetActive(true);
                animButtonsPanel.SetActive(false);
                togglesHandler.HideAll();
            }
            else HideMenu();
        }

        public void ShowLoadingMenu(System.Action onCancelDelegate, string text = null)
        {
            if (loadingText != null)
            {
                if (text == null)
                {
                    loadingText.text = defaultLoadingText;
                }
                else
                {
                    loadingText.text = text;
                }
            }

            this.onCancelDelegate = onCancelDelegate;
            loadingMenu.gameObject.SetActive(true);
            progressText.text = "";
        }

        public void ShowError(string errorMessage)
        {
            errorMenuText.text = errorMessage;
            loadingMenu.gameObject.SetActive(false);
            errorMenu.gameObject.SetActive(true);
            backArrow.gameObject.SetActive(false);
            progressText.text = "";
        }

        public void ShowProgress(float progress)
        {
            progressText.text = progress.ToString("0") + " %";
        }

        public void ExitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnCancel()
        {
            if (onCancelDelegate != null)
            {
                onCancelDelegate();
            }
        }

        #endregion

        #region PLAYER
        public void InitPlayerPanel(GameObject didimoGameObject, string didimoCode, List<DidimoMetadataDataObject> metadata)
        {
            bool isDeformationSupported = DidimoAvatarDataObject.IsDeformationSupported(metadata);
            bool isExpressionSupported = DidimoAvatarDataObject.IsExpressionSupported(metadata);
            bool isBasicRigSupported = DidimoAvatarDataObject.IsBasicRigSupported(metadata);
            bool isRealtimeRigSupported = DidimoAvatarDataObject.IsRealtimeRigSupported(metadata); //this is actually the same as expressions support
            bool isTextToSpeechSupported = DidimoAvatarDataObject.IsTextToSpeechSupported(metadata);

            shouldDisplayHairScroller = true;
            animButtonsPanel.SetActive(true);

            ////Setup UI
            //init hair
            hairConfig.InitHairList(didimoCode);
            togglesHandler.EnableHairOption();

            //init idles (some should be inactive according to template version)
            togglesHandler.EnableIdlesPlayer(isExpressionSupported);

            //init other scrollers
            //eye, haircolor, mocap
            togglesHandler.EnableEyesConfigOption();
            togglesHandler.eyesConfigPanel.SetActive(true);
            eyesScroller.InitializeEyesGalleryScroller();
            togglesHandler.eyesConfigPanel.SetActive(false);

            //init hair color
            togglesHandler.EnableHairColorOption();
            togglesHandler.hairColorConfigPanel.SetActive(true);
            hairColorScroller.InitializeHairColorGalleryScroller();
            togglesHandler.hairColorConfigPanel.SetActive(false);
            hairColorScroller.onItemToggleEventDelegate = (toggle, toggled_index, materialState) =>
            {
                if (toggle.isOn)
                {
                        //Debug.Log("SetHairColorAtIndex " + toggled_index);
                        hairConfig.ChangeHairColorToIndex(toggled_index, materialState);
                }
            };

            ///Load animation system
            
            if (isBasicRigSupported || isRealtimeRigSupported || isExpressionSupported || isTextToSpeechSupported) //isTextToSpeechSupported
            {
                //realtimerig
                LoadingOverlay.Instance.ShowLoadingMenu(() => { }, "Setting up the animation system");

                AnimationPlayer animPlayer = didimoGameObject.GetComponentInChildren<AnimationPlayer>(true);
                animManager.animPlayer = animPlayer;
                animPlayer.interpolateBetweenFrames = false;

                //string basefolderpath = Didimo.Networking.WWWCachedRequest.GetCachePath("", null);
                //string didimoFolderPath = basefolderpath + didimoKey + "/";

                //mocap  (some should display warning according to template version)
                List<string> jsonAnimationTracks = new List<string>();
                //basic rig is supported by both
                //sample animations - to show the amplitude of the supported animation
                jsonAnimationTracks.Add("DidimoImporter/2.0/AnimationFiles/DefaultMocap/simpleROM");
                jsonAnimationTracks.Add("DidimoImporter/2.0/AnimationFiles/DefaultMocap/faceROM");
                //if full range of motion (facial expression) is not supported by the loaded didimo we might need to display a anim-rig mismatch warning when playing such animations
                //init mocap items scroller UI
                mocapScroller.RegisterOnItemToggleEventDelegate((toggle, toggled_index) =>
               {
                   StartCoroutine(PlayMocapAtIndex(toggle.GetComponent<Toggle>(), toggled_index, isBasicRigSupported));
               });
                togglesHandler.mocapPanel.SetActive(true);
                mocapScroller.InitializeMocapRecordingsGalleryScroller(new string[] { }, jsonAnimationTracks.ToArray());
                togglesHandler.mocapPanel.SetActive(false);

                //also init the mocap player
                animManager.SetDefaultAnimationTracksFromPathList(jsonAnimationTracks);
                animManager.Refresh();
                animPlayer.PauseAnimations(true); //the animation player update should not be running when starting
                //enable option on the user interface
                togglesHandler.EnableMocapOption();

                if (isTextToSpeechSupported)
                {
                    speechManager.RealtimeRig = didimoGameObject.GetComponent<Didimo.Animation.RealtimeRig>();
                    //speechManager.SetAnimPlayer(animationPlayer);
                    togglesHandler.EnableSpeechOption();
                }
                else
                {
                    speechManager.RealtimeRig = null;
                    togglesHandler.DisableSpeechOption();
                }

            }
            LoadingOverlay.Instance.Hide();
            pauseToggle.SetIsOnWithoutNotify(false);
            hasDidimoBeenLoaded = true;
        }

        public void ClearAnimButtonsPanel()
        {
            shouldDisplayHairScroller = false;
            hairConfigPanel.SetActive(false);
        }
        public void ShowAnimButtonsConfigPanel()
        {
            animButtonsPanel.SetActive(true);
        }
        public void HideAnimButtonsPanel()
        {
            animButtonsPanel.SetActive(false);
        }
        public void ToggleAnimButtonsPanel()
        {
            if (animButtonsPanel != null)
            {
                if (animButtonsPanel.activeSelf)
                    HideAnimButtonsPanel();
                else if (shouldDisplayHairScroller)
                    ShowAnimButtonsConfigPanel();
            }
        }

        #region ANIMATION

        public IEnumerator PlayMocapAtIndex(Toggle toggle, int index, bool isBasicRigSupported)
        {
            if (toggle.isOn)//if (!mocapPlayer.IsPlaying())
            {

                Debug.Log("PlayMocapAtIndex " + index);
                animManager.PlayAnimationAtIndex(index, () =>
                {
                    toggle.isOn = false;
                    RigAndAnimationMismatchWarningLabel.SetActive(false);
                });

                int defaultMocapAnimIndex = mocapScroller.GetDefaultItemIndex(index);
                if (isBasicRigSupported && defaultMocapAnimIndex != 0) //HARDCODED VALUE OF BASIC RIG ANIM INDEX
                {
                    RigAndAnimationMismatchWarningLabel.SetActive(true);
                }
            }
            else
            {
                animManager.PauseAnimation();
                RigAndAnimationMismatchWarningLabel.SetActive(false);
            }

            yield break;
        }

        public void PlayTextToSpeech()
        {

            //start text-to-speech animation
            //ResetMocapPose();

            /*string textToPlay = speechConfigPanel.activeSelf ? configPlayerText.text : mainPlayerText.text;
            string speechVoiceParam = temporary_speechConfig_langVoice;
            SPEECH_VOICES voice = 0; //SPEECH_VOICES.Brian; //we won't be using this value, as it will be ignored
            int vocalTrackLength = temporary_speechConfig_vocalTractLength;
            int voicePitch = temporary_speechConfig_vocalPitch;
            AlphaPulse onPressPlayWithNoTextAlphaPulse = speechConfigPanel.activeSelf ? configPlayerText.GetComponentInChildren<AlphaPulse>() : mainPlayerText.GetComponentInChildren<AlphaPulse>();
            Debug.Log("TTS - PlayAnimation - text='" + textToPlay + "' speechVoiceParam=" + speechVoiceParam + " vocalTrackLength=" + vocalTrackLength + " voicePitch=" + voicePitch);
            player.PlayAnimation(textToPlay, speechVoiceParam, voice, vocalTrackLength, voicePitch, onPressPlayWithNoTextAlphaPulse);*/
        }

        #endregion //animation

        #endregion //player
    }
}
