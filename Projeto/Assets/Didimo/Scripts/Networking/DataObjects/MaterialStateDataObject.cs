using Didimo.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Didimo.Networking.DataObjects
{
    [Serializable]
    public class MaterialState : DataObject
    {
        public List<Material> materials;
        public Dictionary<string, List<string>> geoMap;

        /// <summary>
        /// Get all texture names. Returns a hash set of a tuple. The second value of the tuple is true when the texture is a diffuse color (should have sRGBTexture color space)
        /// </summary>
        /// <returns>The hash set with the textures.</returns>
        public HashSet<Tuple<string, bool>> GetAllTextureNames()
        {
            // Don't load the same texture twice
            HashSet<Tuple<string, bool>> textureNames = new HashSet<Tuple<string, bool>>();
            foreach (Material material in materials)
            {
                foreach (Material.Parameter parameter in material.parameters)
                {
                    if (parameter.type == "2dTexture")
                    {
                        textureNames.Add(new Tuple<string, bool>((string)parameter.value, parameter.name != "colorSampler"));
                    }
                }
            }

            return textureNames;
        }

        /// <summary>
        /// Get all textures from this material state. 'basePath' will be appended into texture paths to fetch the texture.
        /// Will identify if the texture is a normal map, and handle it appropriately.
        /// WARNING: This should be used only when importing didimos (or materials) during runtime.
        /// </summary>
        /// <param name="basePath">Path to append the path to the textures (texture paths are local).</param>
        /// <returns>A dictionary where they keys are texture names, and values are Texture2D.</returns>
        public Dictionary<string, Texture2D> GetAllTextures(string basePath)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Should only use GetAllTextures in play mode. Make sure you know what you are doing.");
            }

            Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
            HashSet<Tuple<string, bool>> textureNames = GetAllTextureNames();
            foreach (Tuple<string, bool> textureName in textureNames)
            {
                Texture2D texture = null;
                // if normal map
                if (textureName.Item2)
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                }
                else
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, false);
                }
                texture.LoadImage(File.ReadAllBytes(Path.Combine(basePath, textureName.Item1)));
                textures[textureName.Item1] = texture;
            }
            return textures;
        }

        /// <summary>
        /// Set all properties of materials.
        /// </summary>
        /// <param name="textureBasePath">Base path of the textures to import (textures have local path). We will create a full path from textureBasePath + texture local path.</param>
        /// <param name="textures">Dictionary of the textures. See <see cref="GetAllTextures(string)"/>.</param>
        /// <param name="root">The root transform of the didimo. Will search in the hierarchy for the meshes in the geoMap. Pass null if you do not want this to happen.</param>
        /// <param name="importMaterialsIntoProject">If true, will import materials into the project.</param>
        /// <returns>The list of materials created. If importMaterialsIntoProject is true, will return the reference of the material assets in the project."/></returns>
        public List<UnityEngine.Material> SetAllProperties(string textureBasePath, Dictionary<string, Texture2D> textures, Transform root = null, bool importMaterialsIntoProject = false)
        {
            List<UnityEngine.Material> result = new List<UnityEngine.Material>();

            Dictionary<string, UnityEngine.Material> materialsDict = new Dictionary<string, UnityEngine.Material>();

            foreach (MaterialState.Material material in materials)
            {
                Shader shader = Shader.Find(material.shader);
                // If we can't find it by the shader name, search for it under Didimo
                if (shader == null)
                {
                    shader = Shader.Find("Didimo/" + material.shader);
                }

                if (shader == null)
                {
                    Debug.LogError("Could not find shader '" + material.shader + "'");
                    continue;
                }

                UnityEngine.Material unityMaterial = null;
                if (importMaterialsIntoProject)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning("importMaterialsIntoProject should only be used when in editor mode. This will not work when you build/publish your project. Make sure you know what you are doing.");
                    }

                    string materialPath = Path.Combine(textureBasePath, material.name + ".mat");
                    unityMaterial = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
                    if (unityMaterial == null)
                    {
                        unityMaterial = new UnityEngine.Material(shader);
                        AssetDatabase.CreateAsset(unityMaterial, materialPath);
                    }
                    else
                    {
                        unityMaterial.shader = shader;
                    }
#else
                    Debug.LogError("importMaterialsIntoProject is only supported in the editor.");
#endif
                }
                else
                {
                    unityMaterial = new UnityEngine.Material(shader);

                }
                unityMaterial.name = material.name;
                result.Add(unityMaterial);


                if (unityMaterial == null)
                {
                    Debug.LogError("Failed to create material " + material.name + " with shader " + material.shader);
                    continue;
                }

                if (material.parameters != null)
                {
                    foreach (MaterialState.Material.Parameter parameter in material.parameters)
                    {
                        parameter.SetMaterialProperty(unityMaterial, textures);
                    }
                }
                materialsDict.Add(material.name, unityMaterial);
            }

            if (root != null)
            {
                foreach (KeyValuePair<string, List<string>> materialMap in geoMap)
                {
                    foreach (string mesh in materialMap.Value)
                    {
                        GameObject meshGO = root.FindRecursive(mesh).gameObject;
                        Renderer renderer = meshGO.GetComponent<Renderer>();
                        renderer.material = materialsDict[materialMap.Key];
                    }
                }
            }

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif

            return result;
        }


        [Serializable]
        public class Material : DataObject
        {
            public string name;
            public string shader;
            public List<Parameter> parameters;

            [Serializable]
            public class Parameter : DataObject
            {
                public string name;
                public string type;
                public object value;

                public Color GetColor()
                {
                    List<float> array = ((List<object>)value).ConvertAll(x => (float)Convert.ToDouble(x));
                    return new Color(array[0], array[1], array[2]);
                }


                public void SetMaterialProperty(UnityEngine.Material material, Dictionary<string, Texture2D> textures)
                {
                    switch (type)
                    {
                        case "bool":
                        case "enum":
                        case "int":
                        case "long":
                            material.SetInt(name, Convert.ToInt32(value));
                            break;
                        case "float":
                            material.SetFloat(name, (float)Convert.ToDouble(value));
                            break;
                        case "float3":
                            {
                                List<float> array = ((List<object>)value).ConvertAll(x => (float)Convert.ToDouble(x));
                                material.SetVector(name, new Vector3(array[0], array[1], array[2]));
                                break;
                            }
                        case "float4":
                            {
                                List<float> array = ((List<object>)value).ConvertAll(x => (float)x);
                                material.SetVector(name, new Vector4(array[0], array[1], array[2], array[3]));
                                break;
                            }
                        case "2dTexture":
                            material.SetTexture(name, textures[(string)value]);
                            break;
                        default:
                            Debug.LogWarning("Material state doesn't support '" + type + "' type");
                            break;
                    }
                }
            }
        }
    }
}