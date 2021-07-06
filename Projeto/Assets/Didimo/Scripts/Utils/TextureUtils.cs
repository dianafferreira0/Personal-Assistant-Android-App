using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Utils
{
    public class TextureUtils
    {
        /// <summary>
        /// Unity has a custom import process for normal map images. This method simulates that process.
        /// </summary>
        /// <returns>The map from texture.</returns>
        /// <param name="source">Source.</param>
        public static Texture2D NormalMapFromTexture(byte[] bytes)
        {
            Texture2D source = new Texture2D(2, 2, TextureFormat.ARGB32, false, true);
            source.LoadImage(bytes);
            return NormalMapFromTexture(source);
        }
        /// <summary>
        /// Unity has a custom import process for normal map images. This method simulates that process.
        /// </summary>
        /// <returns>The map from texture.</returns>
        /// <param name="source">Source.</param>
        public static Texture2D NormalMapFromTexture(Texture2D texture)
        {
#if !(UNITY_IOS || UNITY_ANDROID) //&& !UNITY_EDITOR

            if (Application.isPlaying)
            {
                Texture2D normalTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false, true);
                Color theColour = new Color();
                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        theColour.r = 0;
                        theColour.g = texture.GetPixel(x, y).g;
                        theColour.b = 0;
                        theColour.a = texture.GetPixel(x, y).r;
                        normalTexture.SetPixel(x, y, theColour);
                    }
                }
                normalTexture.Apply();
                return normalTexture;
            }
#endif
            return texture;
        }

        public static Texture2D TextureFromBytes(byte[] bytes)
        {

            Texture2D skin = new Texture2D(2, 2, TextureFormat.ARGB32, false, false);
            skin.LoadImage(bytes);

            return skin;
        }

        public static Texture2D TextureFromColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            return texture;
        }
    }
}