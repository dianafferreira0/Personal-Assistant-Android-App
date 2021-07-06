using UnityEngine;
using UnityEngine.Video;

namespace Didimo.Animation
{
    public class StreamVideo : MonoBehaviour
    {
        public VideoPlayer videoPlayer;
        public AudioSource audioSource;

        public void StartPlaying()
        {
            videoPlayer.Play();
        }

        public float VideoTotalTime()
        {
#if UNITY_2019_OR_NEWER
        return videoStreamer.videoPlayer.length;
#else
            return videoPlayer.frameCount * videoPlayer.frameRate;
#endif
        }

        public void SetNormalizedTime(float normalizedTime)
        {
            Debug.Log(string.Format("Video: Jumped to frame {0}", (long)(normalizedTime * videoPlayer.frameCount)));
            videoPlayer.time = VideoTotalTime() * normalizedTime;
        }
    }
}
