using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Speech.Menu
{
    [RequireComponent(typeof(Outline))]
    public class AlphaPulse : MonoBehaviour
    {
        public int pulses = 3;
        public float timePerPulse = 0.5f;

        Outline outline;

        private void Start()
        {
            outline = GetComponent<Outline>();
        }

        public void Pulse()
        {
            StopAllCoroutines();
            StartCoroutine(PulseAsync());
        }

        IEnumerator PulseAsync()
        {
            float totalTime = timePerPulse * pulses;
            float currentTime = 0;

            while (currentTime < totalTime)
            {
                outline.effectColor = new Color(outline.effectColor.r, outline.effectColor.g, outline.effectColor.b, (Mathf.Sin(currentTime * Mathf.PI * 2 / timePerPulse - Mathf.PI/2f) + 1) / 2f);

                currentTime += Time.deltaTime;

                yield return new WaitForEndOfFrame();
            }
        }
    }
}