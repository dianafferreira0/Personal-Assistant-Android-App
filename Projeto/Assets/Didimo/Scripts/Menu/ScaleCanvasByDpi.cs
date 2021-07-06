using UnityEngine;


namespace Didimo.Menu
{
    [RequireComponent(typeof(Canvas))]
    public class ScaleCanvasByDpi : MonoBehaviour
    {
        const float kBaseDpi = 180.0f;

        void Start()
        {
            Canvas canvas = GetComponent<Canvas>();

            if (Screen.dpi >= kBaseDpi)
            {
                float scaleFactor = Screen.dpi / kBaseDpi;
                canvas.scaleFactor = scaleFactor;
                canvas.referencePixelsPerUnit /= scaleFactor;
            }
        }
    }
}