using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Animation
{
    public class FPSCounter : MonoBehaviour
    {
        public static float fps = 0; /// potentially useful for querying elsewhere

        public Text fpsText;
        int frameCount = 0;
        float dt = 0.0f;
        public int updatesPerSecond = 4;

        void Update()
        {
            frameCount++;
            dt += Time.deltaTime;
            if (dt > 1.0 / updatesPerSecond)
            {
                fps = (frameCount / dt);
                fpsText.text = fps.ToString("0.00");
                frameCount = 0;
                dt -= 1.0f / updatesPerSecond;
            }
        }
    }
}