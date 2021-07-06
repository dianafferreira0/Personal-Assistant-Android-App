using UnityEngine;

// Based on petersvp's answer in this thread https://answers.unity.com/questions/556913/scaling-resizing-an-image-texture2d.html

/// A unility class with functions to scale Texture2D Data.
///
/// Scale is performed on the GPU using RTT, so it's blazing fast.
/// Setting up and Getting back the texture data is the bottleneck. 
/// But Scaling itself costs only 1 draw call and 1 RTT State setup!
/// WARNING: This script override the RTT Setup! (It sets a RTT!)	 
///
/// Note: This scaler does NOT support aspect ratio based scaling. You will have to do it yourself!
/// It supports Alpha, but you will have to divide by alpha in your shaders, 
/// because of premultiplied alpha effect. Or you should use blend modes.

namespace Didimo.Utils
{
    public class TextureScaler
    {
        /// <summary>
        /// Scales the texture data of the given texture.
        /// </summary>
        /// <param name="tex">Source texure to scale</param>
        /// <param name="maxPixels">Maximum number of pixels</param>
        /// <param name="mode">Filtering mode</param>
        public static bool Scale(Texture2D tex, int maxPixels, FilterMode mode = FilterMode.Trilinear, bool applyMipMaps = false)
        {
            double scaleFactor = System.Math.Sqrt(maxPixels / (double)(tex.width * tex.height));

            int width = Mathf.FloorToInt(tex.width * (float)scaleFactor);
            int height = Mathf.FloorToInt(tex.height * (float)scaleFactor);

            int maxPixelSide = Mathf.Max(width, height);

            if (SystemInfo.maxTextureSize < maxPixelSide)
            {
                Debug.LogWarning("Tried to scale to image to " + width + "x" + height + ", but system only supports a max of " + SystemInfo.maxTextureSize + ". Using system supported size instead.");

                double aspectRatio = (double)width / height;

                if (width > height)
                {
                    width = SystemInfo.maxTextureSize;
                    height = (int)(aspectRatio / (float)width);
                }
                else
                {
                    height = SystemInfo.maxTextureSize;
                    width = (int)(aspectRatio * (float)height);
                }

                maxPixels = SystemInfo.maxTextureSize;
            }

            return Scale(tex, width, height, mode, applyMipMaps);
        }

        /// <summary>
        ///	Returns a scaled copy of given texture. 
        /// </summary>
        /// <param name="tex">Source texure to scale</param>
        /// <param name="width">Destination texture width</param>
        /// <param name="height">Destination texture height</param>
        /// <param name="mode">Filtering mode</param>
        public static Texture2D Scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear, bool applyMipMaps = false)
        {
            Rect texR = new Rect(0, 0, width, height);
            _gpu_scale(src, width, height, mode, applyMipMaps);

            //Get rendered data back to a new texture
            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
            result.Resize(width, height);
            result.ReadPixels(texR, 0, 0, true);
            return result;
        }

        /// <summary>
        /// Scales the texture data of the given texture.
        /// </summary>
        /// <param name="tex">Texure to scale</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        /// <param name="mode">Filtering mode</param>
        public static bool Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear, bool applyMipMaps = false)
        {
            Rect texR = new Rect(0, 0, width, height);
            if (!_gpu_scale(tex, width, height, mode, applyMipMaps))
            {
                return false;
            }

            // Update new texture
            tex.Resize(width, height);
            tex.ReadPixels(texR, 0, 0, true);
            tex.Apply(applyMipMaps);    //Remove this if you hate us applying textures for you :)
            return true;
        }

        // Internal unility that renders the source texture into the RTT - the scaling method itself.
        static bool _gpu_scale(Texture2D src, int width, int height, FilterMode fmode, bool applyMipMaps)
        {
            try
            {
                Debug.Log("Scaling image to " + width + "x" + height);

                //We need the source texture in VRAM because we render with it
                src.filterMode = fmode;
                src.Apply(applyMipMaps);

                //Using RTT for best quality and performance. Thanks, Unity 5
                RenderTexture rtt = new RenderTexture(width, height, 32);

                //Set the RTT in order to render to it
                Graphics.SetRenderTarget(rtt);

                //Setup 2D matrix in range 0..1, so nobody needs to care about sized
                GL.LoadPixelMatrix(0, 1, 1, 0);

                //Then clear & draw the texture to fill the entire RTT.
                GL.Clear(true, true, new Color(0, 0, 0, 0));
                Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.Log("_gpu_scale: caught error " + e.Message);
            }
            return false;
        }
    }
}