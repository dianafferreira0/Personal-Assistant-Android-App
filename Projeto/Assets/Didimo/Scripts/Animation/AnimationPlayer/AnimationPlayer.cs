using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Didimo.Animation.AnimationPlayer
{
    /// <summary>
    /// The didimo animation player, for facial animation.
    /// </summary>
    [RequireComponent(typeof(RealtimeRig))]
    public class AnimationPlayer : MonoBehaviour
    {
        public bool interpolateBetweenFrames = false;
        // Animations we will have pre-loaded
        public List<DidimoAnimation> animationTracks = new List<DidimoAnimation>();

        Dictionary<string, DidimoAnimation> animationNameMap = new Dictionary<string, DidimoAnimation>();

        private RealtimeRig _realtimeRig;
        public RealtimeRig realtimeRig
        {
            get
            {
                if (_realtimeRig == null)
                {
                    _realtimeRig = GetComponent<RealtimeRig>();
                }
                return _realtimeRig;
            }
        }

        private class AnimationPlayingStateChanged : UnityEvent<AnimationPlayer> { }
        private AnimationPlayingStateChanged onAnimationPlayingStateChangedEvent = new AnimationPlayingStateChanged();

        public int autoplayAnimationAtIndex = -1;
        private bool isPaused = false;

        public void SetAnimationTime(string animation, float time)
        {
            DidimoAnimation animationTrack = animationNameMap[animation];
            animationTrack.animationTime = time;
        }

        public void SetNormalizedTime(string animation, float normalizedTime)
        {
            DidimoAnimation animationTrack = animationNameMap[animation];
            animationTrack.NormalizedTime = normalizedTime;
        }

        public void StopAllAnimations()
        {
            foreach (DidimoAnimation animationTrack in animationTracks)
            {
                animationTrack.Stop();
            }
        }

        public bool IsPlaying
        {
            get
            {
                return !isPaused;
            }
        }

        public DidimoAnimation GetAnimation(string animationName)
        {
            return animationNameMap[animationName];
        }

        public int GetAnimationCount()
        {
            return animationTracks.Count;
        }

        public DidimoAnimation GetAnimation(int animationIndex)
        {
            return animationTracks[animationIndex];
        }

        private void Start()
        {
            animationNameMap = new Dictionary<string, DidimoAnimation>();
            for (int i = 0; i < animationTracks.Count; i++)
            {
                // Create an instance of each animation, so we won't refer to the original ones (which may be used across different didimos)
                DidimoAnimation animation = Instantiate(animationTracks[i]);
                animationNameMap[animation.animationName] = animation;
                animationTracks[i] = animation;
                animation.Stop();
            }

            if (autoplayAnimationAtIndex >= 0)
            {
                PlayAnimation(autoplayAnimationAtIndex);
            }
        }

        public void RemoveAllAnimations()
        {
            autoplayAnimationAtIndex = -1;
            animationTracks = new List<DidimoAnimation>();
            animationNameMap = new Dictionary<string, DidimoAnimation>();
            Resources.UnloadUnusedAssets();
        }

        private void CrossFadeAnimation(DidimoAnimation animation, float fadeTime = 0.3f, Action animationReachedEnd = null)
        {
            animation.animationReachedEndAction = animationReachedEnd;
            foreach (DidimoAnimation animationTrack in animationTracks)
            {
                if (!animationTrack.IsStopped)
                {
                    animationTrack.FadeOut(fadeTime);
                }
            }
            animation.FadeIn(fadeTime);
            isPaused = false;
        }

        public void CrossFadeAnimation(string animation, float fadeTime = 0.3f, Action animationReachedEnd = null)
        {
            CrossFadeAnimation(animationNameMap[animation], fadeTime, animationReachedEnd);
        }

        public void CrossFadeAnimation(int animation, float fadeTime = 0.3f, Action animationReachedEnd = null)
        {
            CrossFadeAnimation(animationTracks[animation], fadeTime, animationReachedEnd);
        }

        private void PlayAnimation(DidimoAnimation animation, Action animationReachedEnd = null)
        {
            foreach (DidimoAnimation animationTrack in animationTracks)
            {
                animationTrack.Stop();
            }

            animation.Play();
            animation.animationReachedEndAction = animationReachedEnd;
            if (onAnimationPlayingStateChangedEvent != null)
            {
                onAnimationPlayingStateChangedEvent.Invoke(this);
            }
            isPaused = false;
        }

        /// <summary>
        /// Stops all other animations, and plays the animation with the given name
        /// </summary>
        /// <param name="animation">The name of the animation to play</param>
        /// <param name="animationReachedEnd">The action to be called when the animation ends playing</param>
        public void PlayAnimation(string animation, Action animationReachedEnd = null)
        {
            PlayAnimation(animationNameMap[animation], animationReachedEnd);
        }

        public void PlayAnimation(int animation, Action animationReachedEnd = null)
        {
            PlayAnimation(animationTracks[animation], animationReachedEnd);
        }

        public void SubscribeAnimationPlayStateChanged(UnityAction<AnimationPlayer> action)
        {
            onAnimationPlayingStateChangedEvent.AddListener(action);
        }

        public void UnSubscribeAnimationPlayStateChanged(UnityAction<AnimationPlayer> action)
        {
            onAnimationPlayingStateChangedEvent.RemoveListener(action);
        }

        public void AddAnimationTrack(DidimoAnimation animation)
        {
            if (animation == null)
            {
                Debug.LogWarning("You tried to add an animation that was null");
                return;
            }
            animationTracks.Add(animation);
            animationNameMap[animation.animationName] = animation;
        }

        /// <summary>
        /// Removes the specified animation.
        /// </summary>
        /// <param name="animation">The animation to remove.</param>
        public void DeleteAnimationTrack(DidimoAnimation animation)
        {
            animationNameMap.Remove(animation.animationName);
            animationTracks.Remove(animation);
            Resources.UnloadUnusedAssets();
        }

        //pause, and trigger animation end action if it was set
        public void PauseAnimations(bool pause)
        {
            isPaused = pause;
            onAnimationPlayingStateChangedEvent.Invoke(this);
        }

        public void Update()
        {
            if (isPaused || realtimeRig == null || animationTracks == null)
            {
                return;
            }

            UpdatePose();

            foreach (DidimoAnimation animation in animationTracks)
            {
                animation.Tick(Time.deltaTime);
            }
        }

        /// <summary>
        /// Ticks time according to the specified deltaTime, and then updates the animation pose.
        /// Will update all active animations, even if this AnimationPlayer is paused.
        /// Beware, if you want to show pose for delta time of 0, you must first call UpdatePoseAndTick(0).
        /// </summary>
        /// <param name="deltaTime">The amount, in seconds, which we will be moving each playing animation forward.</param>
        public void UpdatePoseAndTick(float deltaTime)
        {
            foreach (DidimoAnimation animation in animationTracks)
            {
                animation.Tick(deltaTime);
            }

            UpdatePose();
        }

        List<float> facValues = new List<float>();
        public void UpdatePose()
        {
            if (realtimeRig == null)
            {
                Debug.LogWarning("RealtimeRig was null. Disabeling component.");
                enabled = false;
                return;
            }

            if (animationTracks.Count == 0)
            {
                Debug.LogWarning("animationTracks was empty. Disabeling component.");
                enabled = false;
                return;
            }

            // Only reset the realtime rig if we haven't updated it this frame. We might have updated it in another script, and in such a case we won't reset it here.
            // Default argument of ResetAll will ensure this.
            realtimeRig.ResetAll();

            foreach (DidimoAnimation animationTrack in animationTracks)
            {
                if (animationTrack != null && !animationTrack.IsStopped)
                {
                    List<string> facNames = null;

                    if (interpolateBetweenFrames)
                    {
                        animationTrack.GetAnimationFacValues(ref facValues, out facNames);
                    }
                    else
                    {
                        int frame = (int)(animationTrack.NormalizedTime * (animationTrack.GetNumberOfFrames() - 1));
                        animationTrack.GetAnimationFacValuesForFrame(frame, ref facValues, out facNames);
                    }

                    for (int i = 0; i < facValues.Count; i++)
                    {
                        if (facValues[i] != 0)
                        {
                            // Also use additive weight, so as to not override weights we might have set in another class
                            realtimeRig.SetBlendshapeWeightsForFac(facNames[i], facValues[i], true);
                        }
                    }
                }
            }
        }

    }
}
