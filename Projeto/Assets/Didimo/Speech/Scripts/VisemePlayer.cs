using Didimo.Menu;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Speech.Menu;
using Didimo.Utils.Coroutines;
using Didimo.Utils.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Didimo.Animation;

namespace Didimo.Speech
{
    public partial class VisemePlayer : MonoBehaviour
    {
        // Inspector variables
        [SerializeField]
        protected Didimo.Animation.RealtimeRig realtimeRig = null;
        [SerializeField]
        float visemeDuration = 0.3f;
        [SerializeField]
        protected float visemeOffset = 0f;
        [SerializeField]
        float visemeMaxAmplitude = 1f;
        [SerializeField]
        public UnityEngine.Animation animationComponent = null;
        [SerializeField]
        AudioClip audioClip = null;

        [SerializeField]
        TextAsset visemesJson = null;
        [SerializeField]
        protected GameObject visemeDebugPanelTemplate = null;
        [SerializeField]
        public AudioSource audioSource;
        [SerializeField]
        public Transform rootBone = null;
        [SerializeField]
        public InputField textComponent;
        public CoroutineManager coroutineManager;
        protected Dictionary<string, Transform> bones;
        [SerializeField]
        GameObject resetTextBtn = null;

        [SerializeField]
        public Image playActiveImage = null;
        CoroutineManager fadeLayerCoroutineManager;
        [SerializeField]
        public Animator animator = null;

        [SerializeField]
        AlphaPulse onPressPlayWithNoTextAlphaPulse = null;

        public int visemeLayer = 1;
        public float visemeLayerFadeDuration = 0.3f;

        public static SPEECH_VOICES speechVoice = SPEECH_VOICES.Brian; //default voice is male
        [SerializeField] public static int speech_vocalTractLength = 0;
        [SerializeField] public static int speech_pitch = 0;
        private static string speechVoice_param = null;
        public static string SpeechVoice_param
        {
            get
            {
                return speechVoice_param;
            }
        }

        public Didimo.Animation.RealtimeRig RealtimeRig { get => realtimeRig; set => realtimeRig = value; }

        [SerializeField] public bool playAnimationsCycleInLoop = false;
        [SerializeField] public Toggle[] expressionToggles;

        // The values are relative to the bind pose
        protected class AnimationCurve
        {
            public string boneName;
            public Quaternion rotation;
            public Vector3 translation;
            public Vector3 scale;
        }

        protected class AnimationViseme
        {
            //public string name;
            public List<AnimationCurve> curves;
        }

        // Map viseme name into AnimationViseme
        protected Dictionary<string, AnimationViseme> animationVisemes;

        // Private variables
        protected List<Viseme> visemes;
        protected string silentViseme = "sil";
        protected Dictionary<string, Slider> visemeDebugPanels = null;
        public bool didimoIsSpeaking;

        protected class InterpolationViseme
        {
            public InterpolationViseme(string animation, float weight)
            {
                this.animation = animation;
                this.weight = weight;
            }

            public string animation;
            public float weight;
        }

        // Some visemes have uppercase and lower case, and windows doesn't have case sensitive file names.
        // So we cannot have e.g. "t" and "T" animations saved on the file system.
        void FixVisemeNames(List<Viseme> visemes)
        {
            foreach (Viseme viseme in visemes)
            {
                if (!viseme.value.ToLower().Equals(viseme.value))
                {
                    // repeat the viseme name. e.g. "T" becomes "TT"
                    viseme.value += viseme.value;
                }
            }
        }

        AnimationViseme CreateViseme(Viseme viseme)
        {
            bones = new Dictionary<string, Transform>();
            AnimationViseme animationViseme = new AnimationViseme();
            animationViseme.curves = new List<AnimationCurve>();

            AnimationState defaultAnimState = animationComponent[silentViseme];
            defaultAnimState.clip.SampleAnimation(animationComponent.gameObject, 0);

            List<Transform> boneTransforms = new List<Transform>();
            boneTransforms.AddRange(rootBone.GetComponentsInChildren<Transform>());

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                AnimationCurve curve = new AnimationCurve();
                curve.boneName = boneTransforms[i].name;

                curve.translation = boneTransforms[i].localPosition;
                curve.scale = boneTransforms[i].localScale;
                curve.rotation = boneTransforms[i].localRotation;
                animationViseme.curves.Add(curve);
            }

            foreach (Transform t in boneTransforms)
            {
                bones.Add(t.name, t);
            }

            string viseme_value = viseme.value;
            if (string.Compare(viseme_value, "JJ") == 0)
                viseme_value = "i";

            AnimationState visemeAnimState = animationComponent[viseme_value];
            if (visemeAnimState == null)
                visemeAnimState = defaultAnimState;
            visemeAnimState.clip.SampleAnimation(animationComponent.gameObject, visemeAnimState.clip.length);

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                animationViseme.curves[i].translation = boneTransforms[i].localPosition - animationViseme.curves[i].translation;
                animationViseme.curves[i].scale = boneTransforms[i].localScale - animationViseme.curves[i].scale;
                animationViseme.curves[i].rotation = boneTransforms[i].localRotation * Quaternion.Inverse(animationViseme.curves[i].rotation);
            }

            defaultAnimState.clip.SampleAnimation(animationComponent.gameObject, 0);

            return animationViseme;
        }

        protected void CreateAnimationVisemes()
        {
            animationVisemes = new Dictionary<string, AnimationViseme>();

            foreach (Viseme viseme in visemes)
            {
                if (!animationVisemes.ContainsKey(viseme.value))
                {
                    animationVisemes.Add(viseme.value, CreateViseme(viseme));
                }
            }

        }

        // Use this for initialization
        protected void Start()
        {
            if (playActiveImage != null)
                playActiveImage.gameObject.SetActive(false);
            coroutineManager = new GameCoroutineManager();
            fadeLayerCoroutineManager = new GameCoroutineManager();
            visemeDebugPanels = new Dictionary<string, Slider>();

            if (visemeDebugPanelTemplate)
            {
                visemeDebugPanelTemplate.SetActive(false);
            }
        }

        public bool isSpeechPlaying = false;
        public virtual void PlayFromServices(UnityAction<AudioClip> onStartPlayAction, bool showLoading = true)
        {
            isSpeechPlaying = true;
            if (showLoading) LoadingOverlay.Instance.ShowLoadingMenu(() => { coroutineManager.StopAllCoroutines(); }, "Processing...");

            Didimo.Networking.ServicesRequests.GameInstance.GetSpeechFiles(
                coroutineManager,
                textComponent.text,
                speechVoice,
                speech_vocalTractLength,
                speech_pitch,
                (allVisemes, audio) =>
                {
                    if (showLoading) LoadingOverlay.Instance.Hide();
                    coroutineManager.StartCoroutine(PlaySpeechDelayed(1.5f, audio, allVisemes));
                    if (onStartPlayAction != null)
                    {
                        //Set the audio clip, so that the audio recorder can check the number of samples and channels
                        //audioSource.clip = audio;
                        onStartPlayAction(audio);
                    }
                },
                exception =>
                {
                    LoadingOverlay.Instance.Hide();
                    if (!Didimo.Utils.GeneralUtils.InternetStatus())
                        ErrorOverlay.Instance.ShowError("There is no internet connectivity.");
                    else ErrorOverlay.Instance.ShowError(exception.Message);
                    Debug.LogError(exception);
                });
        }


        protected IEnumerator PlaySpeechDelayed(float delay, AudioClip audio, List<Viseme> allVisemes)
        {
            yield return new WaitForSecondsRealtime(delay);

            PlaySpeech(audio, allVisemes);
        }

        void PlayOffline(UnityAction onStartPlayAction)
        {
            if (visemesJson == null)
            {
                Debug.Log("PlayOffline - no text found - ignoring play action");
                return;
            }

            List<Viseme> allVisemes = MiniJSON.Deserialize<List<Viseme>>(visemesJson.text);
            PlaySpeech(audioClip, allVisemes);

            if (onStartPlayAction != null)
            {
                onStartPlayAction();
            }
        }

        /// <summary>
        /// Init this component with the given audio and viseme list.
        /// </summary>
        /// <param name="audio">The audio to play.</param>
        /// <param name="allVisemes">The viseme list, which we will use to animate the didimo's speech.</param>
        public virtual void Init(AudioClip audio, List<Viseme> allVisemes)
        {
            audioSource.clip = audio;
            FixVisemeNames(allVisemes);
            visemes = new List<Viseme>();

            // We do not want "word" visemes, we only want visemes of type "viseme"
            foreach (Viseme viseme in allVisemes)
            {
                if (viseme.type == Viseme.VisemeType.VISEME)
                {
                    visemes.Add(viseme);

                    if (visemeDebugPanelTemplate != null && !visemeDebugPanels.ContainsKey(viseme.value))
                    {
                        GameObject visemePanel = Instantiate(visemeDebugPanelTemplate, visemeDebugPanelTemplate.transform.parent);
                        visemePanel.transform.Find("Text").GetComponent<Text>().text = viseme.value;
                        visemeDebugPanels.Add(viseme.value, visemePanel.GetComponentInChildren<Slider>());
                    }
                }
            }
        }

        public virtual void PlaySpeech(AudioClip audio, List<Viseme> allVisemes)
        {
            Init(audio, allVisemes);

            audioSource.loop = false;

            audioSource.Play();
            didimoIsSpeaking = true;
            if (playActiveImage != null)
                playActiveImage.gameObject.SetActive(true);
        }

        public void PlayAnimation()
        {
            if (gameObject.activeInHierarchy)
            {
                if (string.IsNullOrEmpty(textComponent.text))
                {
                    onPressPlayWithNoTextAlphaPulse.Pulse();
                }
                else
                {
                    PlayFromServices(null);
                }
            }
        }

        public void JustPlay()
        {
            PlayOffline(null);
        }

        public virtual void StopAnimation()
        {
            if (coroutineManager != null) coroutineManager.StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();
            if (playActiveImage != null) playActiveImage.gameObject.SetActive(false);
        }

        public virtual void ForceStopAnimationAndResetExpression()
        {
            if (!isSpeechPlaying)
                return;
            StopAnimation();
            didimoIsSpeaking = false;
            isSpeechPlaying = false;
        }

        protected List<InterpolationViseme> GetVisemesForInterpolation(float time)
        {
            List<InterpolationViseme> result = new List<InterpolationViseme>();

            for (int i = 0; i < visemes.Count; i++)
            {
                Viseme currentViseme = visemes[i];

                float previousVisemeTime = i > 0 ? visemes[i - 1].timeInSeconds : 0;
                float nextVisemeTime = i < visemes.Count - 1 ? visemes[i + 1].timeInSeconds : visemes[visemes.Count - 1].timeInSeconds + visemeDuration;

                float fadeInDuration = visemeDuration / 2f;
                if ((currentViseme.timeInSeconds - previousVisemeTime) < (visemeDuration / 2f))
                {
                    fadeInDuration = (currentViseme.timeInSeconds - previousVisemeTime);
                }

                float fadeOutDuration = visemeDuration / 2f;
                if ((nextVisemeTime - currentViseme.timeInSeconds) < (visemeDuration / 2f))
                {
                    fadeOutDuration = (nextVisemeTime - currentViseme.timeInSeconds);
                }

                if (time >= (currentViseme.timeInSeconds - fadeInDuration) &&
                    time <= (currentViseme.timeInSeconds + fadeOutDuration))
                {
                    float weight;

                    // Fade in
                    if (time < currentViseme.timeInSeconds)
                    {
                        weight = (time - (currentViseme.timeInSeconds - fadeInDuration)) / fadeInDuration;

                    } // Fade out
                    else
                    {
                        weight = ((currentViseme.timeInSeconds + fadeOutDuration) - time) / fadeOutDuration;
                    }

                    // Interpolation function ( smooth step)
                    weight = weight * weight * (3f - 2f * weight);

                    weight *= visemeMaxAmplitude;

                    // If we don't have the desired time to interpolate the viseme, we have to lower its weight
                    // This will cause the interpolation to be smooth, and the viseme will never reach the full weight
                    weight *= Mathf.Min(fadeInDuration, fadeOutDuration) / (visemeDuration / 2f);

                    if (weight <= 0.0001f)
                    {
                        continue;
                    }

                    result.Add(new InterpolationViseme(currentViseme.value, weight));
                }
            }

            if (result.Count > 2)
            {
                Debug.Log("WHAT " + result.Count);
            }

            return result;
        }

        /// <summary>
        /// Update the pose of the didimo for the give time, in seconds. 
        /// Note that the animation clip of the audio source must be defined for this to work.
        /// Returns true if successful, false otherwise (if animations reached the end, or the cilp is null).
        /// IMPORTANT: <see cref="Init(AudioClip, List{Viseme})"/> must be called first, to set the audio clip and viseme list.
        /// </summary>
        /// <param name="time">The current time, in seconds, of the animation pose.</param>
        /// <returns>True if the pose was updated with success. False otherwise.</returns>
        public virtual bool UpdatePoseForTime(float time)
        {
            if (audioSource.clip == null || time > audioSource.clip.length)
            {
                return false;
            }

            UpdatePoseForTimeImpl(time);
            return true;
        }

        protected void UpdatePoseForTimeImpl(float time)
        {
            List<InterpolationViseme> visemesToInterpolate = GetVisemesForInterpolation(time + visemeOffset);

            realtimeRig.ResetAll();
            foreach (InterpolationViseme interpolationViseme in visemesToInterpolate)
            {
                PlayMatchingRealtimeRigVisemeFromPhoneme(interpolationViseme.animation, interpolationViseme.weight);
            }
        }

        protected virtual void Update()
        {
            // Reset the pose on update. If we have other animations playing (e.g. idle animations), this won't override them
            if (didimoIsSpeaking && audioSource.isPlaying)
            {
                UpdatePoseForTimeImpl(audioSource.time);
            }
        }

        public void PlayMatchingRealtimeRigVisemeFromPhoneme(string phoneme, float weight)
        {
            Debug.Log(phoneme + " " + weight);
            string viseme_name;
            // map between thename of the phone and the name of the viseme in the FACS we support in the realtime rig
            // (switch-case tables are compiled to constant hash jump tables)
            switch (phoneme)
            {
                case "sil": viseme_name = ""; break;
                case "p": viseme_name = "phoneme_p_b_m"; break;
                case "t": viseme_name = "phoneme_d_t_n"; break;
                case "SS": viseme_name = "phoneme_s_z"; break;
                case "TT": viseme_name = "phoneme_d_t_n"; break;
                case "f": viseme_name = "phoneme_f_v"; break;
                case "k": viseme_name = "phoneme_k_g_ng"; break;
                case "i": viseme_name = "phoneme_ay"; break;
                case "r": viseme_name = "phoneme_r"; break;
                case "s": viseme_name = "phoneme_s_z"; break;
                case "u": viseme_name = "phoneme_ey_eh_uh"; break;
                case "&": viseme_name = "phoneme_aa"; break;
                case "@": viseme_name = "phoneme_aa"; break;
                case "a": viseme_name = "phoneme_ae_ax_ah"; break;
                case "e": viseme_name = "phoneme_ey_eh_uh"; break;
                case "EE": viseme_name = "phoneme_ao"; break;
                case "o": viseme_name = "phoneme_ao"; break;
                case "OO": viseme_name = "phoneme_aw"; break;
                default: viseme_name = ""; break;
            }

            realtimeRig.SetBlendshapeWeightsForFac(viseme_name, weight, true);
        }

        // Called after animations are computed
        protected virtual void LateUpdate()
        {
            if (didimoIsSpeaking && !audioSource.isPlaying)
            {
                didimoIsSpeaking = false;
                if (playActiveImage != null)
                    playActiveImage.gameObject.SetActive(false);

                isSpeechPlaying = false;
            }
        }

        protected void UpdateVisemeDebugPanel(string visemeName, float weight)
        {
            if (visemeDebugPanelTemplate != null)
            {
                if (visemeDebugPanels.ContainsKey(visemeName))
                {
                    visemeDebugPanels[visemeName].value = weight;
                }
            }
        }

        public bool CanShare()
        {
            return gameObject.activeInHierarchy && !string.IsNullOrEmpty(textComponent.text);
        }

        public void ResetText()
        {
            textComponent.text = "";
            resetTextBtn.SetActive(false);
        }

        public void TextChanged()
        {
            if (textComponent.text != "")
            {
                resetTextBtn.SetActive(true);
            }
            else
            {
                resetTextBtn.SetActive(false);
            }
        }

        public static void SetVoiceFromString(string voice_name)
        {
            speechVoice_param = voice_name;
        }

        protected void FadeVisemeLayer(bool fadeIn)
        {
            fadeLayerCoroutineManager.StopAllCoroutines();
            fadeLayerCoroutineManager.StartCoroutine(FadeVisemeLayerAsync(fadeIn));
        }

        IEnumerator FadeVisemeLayerAsync(bool fadeIn)
        {
            if (animator == null)
                yield break;
            float startWeight = animator.GetLayerWeight(visemeLayer);
            float time = 0;
            float targetWeight = fadeIn ? 1f : 0f;

            while (time < visemeLayerFadeDuration)
            {
                yield return new WaitForEndOfFrame();

                time += Time.deltaTime;

                animator.SetLayerWeight(visemeLayer, Mathf.SmoothStep(startWeight, targetWeight, time / visemeLayerFadeDuration));
            }

            animator.SetLayerWeight(visemeLayer, targetWeight);
        }

        /*public void OnValueChanged_VocalTractLength(Slider slider)
        {
            coroutineManager.StopAllCoroutines();
            coroutineManager.StartCoroutine(OnNextMouseRelease(() => {
                StopAnimation();
                speech_vocalTractLength = (int)slider.value;
                PlayAnimation();
            }));
        }

        public void OnValueChanged_Pitch(Slider slider)
        {
            coroutineManager.StopAllCoroutines();
            coroutineManager.StartCoroutine(OnNextMouseRelease(() => {
                StopAnimation();
                speech_pitch = (int)slider.value;
                PlayAnimation();
            }));
        }*/

        IEnumerator OnNextMouseRelease(UnityAction action)
        {
            while (Input.GetMouseButton(0))
            {
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();

            action();
        }

    }
}
