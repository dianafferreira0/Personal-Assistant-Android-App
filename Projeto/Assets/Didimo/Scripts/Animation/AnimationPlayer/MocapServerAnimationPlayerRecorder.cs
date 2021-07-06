using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Didimo.Animation.AnimationPlayer
{
    public class MocapServerAnimationPlayerRecorder : MocapServerAnimationPlayer
    {
        enum RecorderState
        {
            PLAYING,
            RECORDING,
            REPLAYING
        }

        private RecorderState recorderState = RecorderState.PLAYING;
        public AudioSource audioSource;
        private AudioClip audioClip;
        protected List<FACSample> facSamples;
        // Used when recording
        protected List<AudioSample> audioSamples;
        float animationTime;
        float totalTime;

        public UnityEvent onPlayEndedEvent;

        public List<AudioSample> GetAudioSamplesForVideo(out int length)
        {
            length = 0;
            List<AudioSample> result = new List<AudioSample>();
            if (audioSamples != null)
            {
                for (int i = 0; i < audioSamples.Count; i++)
                {
                    if (audioSamples[i].timeStamp >= facSamples[0].timeStamp)
                    {
                        AudioSample sample = new AudioSample(audioSamples[i].audio, audioSamples[i].timeStamp - facSamples[0].timeStamp);
                        result.Add(sample);
                    }
                }
                if (result.Count > 0)
                {
                    length = (int)((result[result.Count - 1].timeStamp - result[0].timeStamp) * 1e-6 * 44100) + result[result.Count - 1].audio.Length;
                }
            }

            return result;
        }

        public void StartRecording()
        {
            recorderState = RecorderState.RECORDING;
        }

        public void StartPlayback()
        {
            animationTime = 0;
            totalTime = (float)((facSamples[facSamples.Count - 1].timeStamp - facSamples[0].timeStamp) * 1e-6);
            recorderState = RecorderState.REPLAYING;

            int lengthSamples;
            List<AudioSample> videoAudioSamples = GetAudioSamplesForVideo(out lengthSamples);
            audioClip = AudioClip.Create("audio", lengthSamples, 1, 44100, false);
            for (int i = 0; i < videoAudioSamples.Count; i++)
            {
                audioClip.SetData(videoAudioSamples[i].audio, (int)((videoAudioSamples[i].timeStamp - videoAudioSamples[0].timeStamp) * 1e-6 * 44100.0f));
            }
            audioSource.clip = audioClip;
            audioSource.PlayDelayed((float)(videoAudioSamples[0].timeStamp * 1e-6));
        }

        public override void OnGetServerFacs(float[] facs, ulong timeStamp)
        {
            switch (recorderState)
            {
                case RecorderState.PLAYING:
                    base.OnGetServerFacs(facs, timeStamp);
                    break;

                case RecorderState.RECORDING:
                    base.OnGetServerFacs(facs, timeStamp);
                    FACSample fACSample = new FACSample(facs, timeStamp);
                    facSamples.Add(fACSample);
                    break;

                case RecorderState.REPLAYING:
                    // Do nothing!
                    break;
            }
        }

        public override void OnGetServerAudio(short[] audio, ulong timeStamp)
        {
            if (recorderState == RecorderState.RECORDING)
            {
                AudioSample audioSample = new AudioSample(audio, timeStamp);
                audioSamples.Add(audioSample);
            }
        }

        private void Update()
        {
            if (recorderState == RecorderState.REPLAYING)
            {
                int currentFrame = (int)((animationTime / totalTime) * facSamples.Count);
                animationTime += Time.deltaTime;

                if (currentFrame >= facSamples.Count)
                {
                    animationTime = 0;
                    currentFrame = 0;

                    if (onPlayEndedEvent != null)
                    {
                        onPlayEndedEvent.Invoke();
                    }

                    recorderState = RecorderState.PLAYING;
                }
                for (int i = 0; i < serverFacNames.Length; i++)
                {
                    float facValue = Mathf.Clamp(facSamples[currentFrame].facs[i], 0.0f, 1.0f);
                    realtimeRig.SetBlendshapeWeightsForFac(serverFacNames[i], facValue);
                }
            }
        }
    }
}