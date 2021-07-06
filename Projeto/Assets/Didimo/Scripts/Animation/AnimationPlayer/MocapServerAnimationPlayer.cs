using UnityEngine;


namespace Didimo.Animation.AnimationPlayer
{
    public class MocapServerAnimationPlayer : MonoBehaviour, IMocapServerEventHandler
    {
        protected string[] serverFacNames;
        public RealtimeRig realtimeRig;

        private void Awake()
        {
            if (!realtimeRig)
            {
                Debug.LogError("Realtime rig property not set in inspector of MocapServerAnimationPlayer. Disabling component...");
                enabled = false;
            }
        }

        public class ISample
        {
            public static int Compare(ISample value1, ISample value2)
            {
                return value1.timeStamp.CompareTo(value2.timeStamp);
            }

            public ulong timeStamp;
        }

        protected class FACSample : ISample
        {
            public FACSample(float[] f, ulong t)
            {
                facs = f;
                timeStamp = t;
            }
            public float[] facs;
        }

        public class AudioSample : ISample
        {
            float[] PCM2Floats(byte[] bytes)
            {
                // See pcm2float in https://github.com/mgeier/python-audio/blob/master/audio-files/utility.py
                float max = -(float)System.Int16.MinValue;
                float[] samples = new float[bytes.Length / 2];

                for (int i = 0; i < samples.Length; i++)
                {
                    short int16sample = System.BitConverter.ToInt16(bytes, i * 2);
                    samples[i] = (float)int16sample / max;
                }

                return samples;
            }
            public AudioSample(float[] a, ulong t)
            {
                audio = a;
                timeStamp = t;
            }

            public AudioSample(short[] a, ulong t)
            {
                byte[] audioBytes = new byte[a.Length * sizeof(short)];
                System.Buffer.BlockCopy(a, 0, audioBytes, 0, audioBytes.Length);
                timeStamp = t;
                audio = PCM2Floats(audioBytes);
            }
            public float[] audio;
        }

        public void OnGetServerFacNames(string[] facNames)
        {
            serverFacNames = facNames;
            Debug.Log("Number of fac names: " + facNames.Length);
        }

        public virtual void OnGetServerFacs(float[] facs, ulong timeStamp)
        {
            //Debug.Log("Number of facs: " + facs.Length);
            SetDefaultAnimationState();
            for (int i = 0; i < facs.Length; i++)
            {
                if (!realtimeRig.SetBlendshapeWeightsForFac(serverFacNames[i], facs[i]))
                {
                    Debug.LogWarning("Failed to get animation state for animation " + serverFacNames[i]);
                }
            }
        }

        public virtual void OnGetServerAudio(short[] audio, ulong timeStamp)
        {
            // we are not playing audio here, only animation
        }

        void SetDefaultAnimationState()
        {
            realtimeRig.ResetAll();
        }
    }
}
