using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Animation.AnimationPlayer
{
    public class AllFacsSequenceAnimationPlayer : MonoBehaviour
    {
        private float currentTime = 0;
        public float secondsPerFac = 1.0f;
        private float totalTime;
        public RealtimeRig realtimeRig;

        private List<string> facNames;

        // Start is called before the first frame update
        void Start()
        {
            currentTime = 0;
            facNames = realtimeRig.GetSupportedFACS();

            totalTime = facNames.Count * secondsPerFac;
        }

        // Update is called once per frame
        void Update()
        {
            int currentFac = Mathf.Clamp((int)(currentTime / secondsPerFac), 0, facNames.Count - 1);
            float animationTime = (currentTime / secondsPerFac) - currentFac;
            animationTime = Mathf.Sin(animationTime * Mathf.PI);

            realtimeRig.ResetAll();
            realtimeRig.SetBlendshapeWeightsForFac(facNames[currentFac], animationTime);

            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
            {
                currentTime = 0;
            }
        }
    }
}
