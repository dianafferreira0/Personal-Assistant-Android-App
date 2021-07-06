using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Animation.AnimationPlayer
{
    public class AllBlendshapeSequenceAnimationPlayer : MonoBehaviour
    {
        private float currentTime = 0;
        public float secondsPerBlendshape = 1.0f;
        private float totalTime;
        public RealtimeRig realtimeRig;
        public Text blendshapeName;

        private List<string> blendshapeNames;
        // Start is called before the first frame update
        void Start()
        {
            currentTime = 0;
            blendshapeNames = realtimeRig.GetBlendshapeNames();
            totalTime = blendshapeNames.Count * secondsPerBlendshape;
        }

        // Update is called once per frame
        void Update()
        {
            int currentBlendshape = Mathf.Clamp((int)(currentTime / secondsPerBlendshape), 0, blendshapeNames.Count - 1);
            float animationTime = (currentTime / secondsPerBlendshape) - currentBlendshape;
            animationTime = Mathf.Sin(animationTime * Mathf.PI);

            //realtimeRig.ResetAll();
            realtimeRig.SetBlendshapeWeight(blendshapeNames[currentBlendshape], animationTime, false);

            blendshapeName.text = blendshapeNames[currentBlendshape];

            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
            {
                currentTime = 0;
            }
        }
    }
}
