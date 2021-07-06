using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Animation.AnimationPlayer.UI
{
    public class AnimationTimeSlider : MonoBehaviour
    {

        public AnimationPlayer animationPlayer;
        public Slider timeSlider;
        public Text frameNumberText;

        private bool shouldSetNormalizedTime = true;
        private bool wasPlayingBeforeValueChange = true;
        private bool isDragging = false;
        public int animationToTrack = 0;

        private void Start()
        {
            timeSlider.onValueChanged.AddListener(OnValueChangedAction);
        }

        void OnValueChangedAction(float value)
        {
            if (shouldSetNormalizedTime)
            {
                animationPlayer.GetAnimation(animationToTrack).NormalizedTime = value;
                if (!isDragging)
                {
                    wasPlayingBeforeValueChange = animationPlayer.IsPlaying;
                    animationPlayer.PauseAnimations(true);
                    isDragging = true;
                }
                else
                {
                    animationPlayer.UpdatePose();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (isDragging && !Input.GetMouseButton(0))
            {
                animationPlayer.PauseAnimations(!wasPlayingBeforeValueChange);
                isDragging = false;
            }
            shouldSetNormalizedTime = false;
            if (animationPlayer.GetAnimationCount() > 0)
            {
                timeSlider.value = animationPlayer.GetAnimation(animationToTrack).NormalizedTime;
            }
            if (frameNumberText != null)
            {
                frameNumberText.text = ((int)(animationPlayer.GetAnimation(animationToTrack).NormalizedTime * (animationPlayer.GetAnimation(animationToTrack).GetNumberOfFrames() - 1)) + 1).ToString();
            }
            shouldSetNormalizedTime = true;
        }
    }
}
