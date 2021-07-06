using System.Collections;
using UnityEngine;


namespace Didimo.Menu
{
    public class UISlideToCenterOfScreen : MonoBehaviour
    {

        public float slideAnimationDuration = 0.3f;
        bool slidToCenter;
        Vector3 oldPosition;

        void SlideToCenterOfScreen()
        {
            if (slidToCenter)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(PerformSlideAnimation(true));
        }

        public void SlideBack()
        {
            if (!slidToCenter)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(PerformSlideAnimation(false));
        }

        IEnumerator PerformSlideAnimation(bool toCenter)
        {
            float currentTime = 0.0f;

            Vector3 targetPosition;

            if (toCenter)
            {
                targetPosition = transform.position;
                targetPosition.y = Screen.height / 2f;
            }
            else
            {
                targetPosition = oldPosition;
            }

            oldPosition = transform.position;

            while (currentTime < slideAnimationDuration)
            {
                yield return new WaitForEndOfFrame();
                currentTime += Time.deltaTime;

                transform.position = Vector3.Lerp(oldPosition, targetPosition, Mathf.SmoothStep(0, 1, currentTime));
            }

            slidToCenter = toCenter;
            transform.position = targetPosition;
        }
    }
}