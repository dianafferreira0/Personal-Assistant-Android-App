using System;
using System.Collections;
using UnityEngine;

namespace Didimo.Utils
{
    /// <summary>
    /// Monobehaviour used to take screenshots from a camera. 
    /// This will disable the camera and will only render to a render texture when <see cref="TakeScreenShot(int, int)"/> is called.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraScreenShot : MonoBehaviour
    {
        Camera screenshotCamera;

        public Camera CameraComponent
        {
            get
            {
                return screenshotCamera;
            }
        }

        private void Start()
        {
            screenshotCamera = GetComponent<Camera>();
            screenshotCamera.enabled = false;
        }


        /// <summary>
        /// Take a screenshot. The camera's aspect ratio will be set to match the given ratio (width / height).
        /// </summary>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <returns>The Texture2D screenshot with the given aspect ratio.</returns>
        public Texture2D TakeScreenshot(int width, int height)
        {

            screenshotCamera.aspect = width / height;
            RenderTexture rt = new RenderTexture(width, height, 24);
            screenshotCamera.targetTexture = rt;
            RenderTexture.active = rt;
            screenshotCamera.Render();
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            screenshotCamera.targetTexture = null;
            RenderTexture.active = null;
            return screenShot;
        }
    }
}