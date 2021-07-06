using Didimo.Animation.DataObjects;
using Didimo.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Didimo.Animation.AnimationPlayer
{
    // Unity can't serialize dictionaries, so we need this hack.
    [Serializable]
    public class SerializableDictionary<K, V>
    {
        [SerializeField]
        List<K> keys;
        [SerializeField]
        List<V> values;

        Dictionary<K, V> dictionary = null;

        public SerializableDictionary()
        {
            keys = new List<K>();
            values = new List<V>();
        }

        public SerializableDictionary(Dictionary<K, V> dictionary)
        {
            keys = new List<K>();
            values = new List<V>();

            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void BuilDictionaryIfRequired()
        {
            if (dictionary == null || dictionary.Count != keys.Count)
            {
                dictionary = new Dictionary<K, V>();
                for (int i = 0; i < keys.Count; i++)
                {
                    dictionary[keys[i]] = values[i];
                }
            }
        }

        public bool ContainsKey(K key)
        {
            BuilDictionaryIfRequired();
            return dictionary.ContainsKey(key);
        }

        public V this[K index]
        {
            get
            {
                BuilDictionaryIfRequired();
                return dictionary[index];
            }
        }

        public Dictionary<K, V> GetDictionary()
        {
            BuilDictionaryIfRequired();
            return dictionary;
        }

        public void Add(K key, V value)
        {
            BuilDictionaryIfRequired();

            dictionary[key] = value;
            keys.Add(key);
            values.Add(value);
            dictionary = new Dictionary<K, V>();
        }
    }

    [Serializable]
    public class StrFloatDictionary : SerializableDictionary<string, float>
    {
        public StrFloatDictionary(Dictionary<string, float> dictionary) : base(dictionary)
        {
        }
    }
    [Serializable]
    public class FacsDrivingBlendshapesDicionary : SerializableDictionary<string, StrFloatDictionary>
    {
    }

    [Serializable]
    public class MeshDrivingBlendshapes
    {
        public string drivenMeshName;
        [SerializeField]
        private FacsDrivingBlendshapesDicionary facsDrivingBlendshapes;

        // Holds:
        // - the initial position and rotation of the bones that are closest to each of the bones that we will animate
        // - the initial vertex and normal, for the vertex that controlls the bone
        // - the vertex and normal offsets, for the vertex that constrolls the bone
        [SerializeField]
        private List<RigControlsSnapshot> rigControlsState = new List<RigControlsSnapshot>();

        public List<Blendshape> blendshapes;

        Dictionary<string, Blendshape> blendshapesDic;

        public List<string> GetFacNames()
        {
            return new List<string>(facsDrivingBlendshapes.GetDictionary().Keys);
        }

        public List<string> GetBlendshapeNames()
        {
            List<string> result = new List<string>();
            foreach (Blendshape blendshape in blendshapes)
            {
                result.Add(blendshape.name);
            }

            return result;
        }

        /// <summary>
        /// Build this RealtimeRig's MeshDrivingBlendshape.
        /// </summary>
        /// <param name="blendShapedMeshDataObject">The Data Object to build this MeshDrivingBlendshape.</param>
        /// <param name="transform">The transform that should at least be at the root level of the didimo skeleton. Will search the hierarchy for bones.</param>
        public MeshDrivingBlendshapes(RealtimeRigDataObject.BlendShapedMeshDataObject blendShapedMeshDataObject, Transform drivenMeshTransform, Transform transform, float scale)
        {
            drivenMeshName = drivenMeshTransform.name;
            facsDrivingBlendshapes = new FacsDrivingBlendshapesDicionary();

            foreach (var driver in blendShapedMeshDataObject.blendshape_relation_drivers)
            {
                StrFloatDictionary entry = new StrFloatDictionary(driver.Value);
                facsDrivingBlendshapes.Add(driver.Key, entry);
            }

            //blendshape_relation_drivers_file_txt = MiniJSON.Serialize(blendShapedMeshDataObject.blendshape_relation_drivers);
            Debug.Assert(blendShapedMeshDataObject.bone_driver_names.Count == blendShapedMeshDataObject.driven_vertex_indices.Count);
            rigControlsState = new List<RigControlsSnapshot>();
            //bone_driver_indices = new List<int>();
            for (int i = 0; i < blendShapedMeshDataObject.bone_driver_names.Count; i++)
            {
                //int driverVertexIndex = blendShapedMeshDataObject.driven_vertex_indices[i];
                string drivenBoneName = blendShapedMeshDataObject.bone_driver_names[i];

                Transform boneTransform = transform.FindRecursive(drivenBoneName);
                if (boneTransform == null)
                {
                    Debug.LogError("Failed to find bone named '" + drivenBoneName + "'");
                }
                else
                {
                    RigControlsSnapshot rigControlsSnapshot = new RigControlsSnapshot();
                    rigControlsSnapshot.targetBoneName = boneTransform.name;
                    rigControlsSnapshot.initialBonePos = boneTransform.localPosition;
                    rigControlsSnapshot.initialBoneRotation = boneTransform.localRotation;
                    rigControlsSnapshot.bindRotation = (transform.localToWorldMatrix * boneTransform.worldToLocalMatrix).rotation;

                    rigControlsState.Add(rigControlsSnapshot);
                }
            }

            blendshapes = new List<Blendshape>();

            if (blendShapedMeshDataObject.blendshapes != null)
            {
                foreach (KeyValuePair<string, RealtimeRigDataObject.Blendshape> dataObjectBlendshape in blendShapedMeshDataObject.blendshapes)
                {
                    // Get the blendshape name, without the "_w###" final part, which indicates the max weight of the in-between
                    string blendshapeName = Regex.Replace(dataObjectBlendshape.Key, "_w[0-9][0-9][0-9]$", "");
                    // if we can match with "_w###", at the end of the name, match with the ### part (weight between 0 and 100), and convert it to a float between 0.0 and 1.0
                    Match match = Regex.Match(dataObjectBlendshape.Key, "(?<=_w)[0-9][0-9][0-9]$");
                    float inBetweenMaxWeight;
                    if (match.Success)
                    {
                        inBetweenMaxWeight = float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) / 100.0f;
                    }
                    else
                    {
                        inBetweenMaxWeight = 1.0f;
                    }

                    Blendshape blendshape = blendshapes.Find(i => i.name == blendshapeName);
                    if (blendshape == null)
                    {
                        blendshape = new Blendshape();
                        blendshape.name = blendshapeName;
                        blendshape.inBetweens = new List<Blendshape.InBetween>();
                        blendshapes.Add(blendshape);
                    }
                    Blendshape.InBetween inBetween = new Blendshape.InBetween(inBetweenMaxWeight);

                    for (int i = 0; i < dataObjectBlendshape.Value.vertOffset.Count; i += 3)
                    {
                        inBetween.vertexOffsets.Add(new Vector3(-dataObjectBlendshape.Value.vertOffset[i],
                            dataObjectBlendshape.Value.vertOffset[i + 1],
                            dataObjectBlendshape.Value.vertOffset[i + 2]) / scale);
                    }

                    for (int i = 0; i < dataObjectBlendshape.Value.normalOffset.Count; i += 4)
                    {
                        Quaternion normalRotation = new Quaternion(
                            dataObjectBlendshape.Value.normalOffset[i],
                            -dataObjectBlendshape.Value.normalOffset[i + 1],
                            -dataObjectBlendshape.Value.normalOffset[i + 2],
                            dataObjectBlendshape.Value.normalOffset[i + 3]);

                        Quaternion rotationOffset = normalRotation;
                        inBetween.normalOffsets.Add(rotationOffset);
                    }

                    blendshape.inBetweens.Add(inBetween);
                }
            }

            // sort in-betweens
            foreach (Blendshape blendshape in blendshapes)
            {
                blendshape.inBetweens.Sort((a, b) =>
                {
                    return a.finalWeight.CompareTo(b.finalWeight);
                });
            }
        }

        public void Init(Transform root)
        {
            blendshapesDic = new Dictionary<string, Blendshape>();

            foreach (Blendshape blendshape in blendshapes)
            {
                blendshapesDic[blendshape.name] = blendshape;
            }

            foreach (RigControlsSnapshot snapshot in rigControlsState)
            {
                snapshot.targetBone = root.FindRecursive(snapshot.targetBoneName);
            }

            facsDrivingBlendshapes.BuilDictionaryIfRequired();

            foreach (var entry in facsDrivingBlendshapes.GetDictionary().Keys)
            {
                facsDrivingBlendshapes.GetDictionary()[entry].BuilDictionaryIfRequired();
            }
        }

        public void UpdateBonePositions()
        {
            // Update the transform (position and rotation) on the bones through the blendshape mesh, according to the changes in the blendshaped mesh
            // Apply the transforms by the order of the hierarchy of bones
            for (int i = 0; i < rigControlsState.Count; i++)
            {
                RigControlsSnapshot rigControl = rigControlsState[i];
                rigControl.targetBone.localPosition = rigControl.initialBonePos + rigControl.bindRotation * rigControl.vertexOffset;
                // Apply rotation that transforms from object (blendshaped mesh) space, into local bone space
                rigControl.targetBone.localRotation = rigControl.initialBoneRotation * rigControl.normalOffset;
            }
        }

        public void CalculateComposedBlendshape()
        {
            // Reset the current shape
            for (int i = 0; i < rigControlsState.Count; i++)
            {
                rigControlsState[i].vertexOffset = Vector3.zero;
                rigControlsState[i].normalOffset = Quaternion.identity;
            }

            for (int blendshapeIndex = 0; blendshapeIndex < blendshapes.Count; blendshapeIndex++)
            {
                if (blendshapes[blendshapeIndex].weight != 0)
                {
                    for (int i = 0; i < rigControlsState.Count; i++)
                    {
                        rigControlsState[i].vertexOffset +=
                            blendshapes[blendshapeIndex].GetVertexOffsetForCurrentWeight(i);

                        rigControlsState[i].normalOffset *=
                            blendshapes[blendshapeIndex].GetNormalOffsetForCurrentWeight(i);
                    }
                }
            }
        }

        // Update blendshape weights according to active FACS
        public void UpdateBlendshapeWeights(Dictionary<string, float> facWeights)
        {
            // First, reset all blendshape weights
            foreach (Blendshape blendshape in blendshapes)
            {
                blendshape.weight = 0;
            }

            // Then calculate their values, additively
            foreach (KeyValuePair<string, float> facWeight in facWeights)
            {
                if (facWeight.Value != 0 && facsDrivingBlendshapes.ContainsKey(facWeight.Key))
                {
                    foreach (var weightedBlendshapes in facsDrivingBlendshapes.GetDictionary()[facWeight.Key].GetDictionary())
                    {
                        SetBlendshapeWeight(weightedBlendshapes.Key, facWeight.Value * weightedBlendshapes.Value, true);
                    }
                }
            }
        }

        public bool SetBlendshapeWeight(string blendshapeName, float weight, bool additive)
        {
            string fixedName = blendshapeName.Replace("levelTwo_", "");
            if (!blendshapesDic.ContainsKey(fixedName))
            {
                //Debug.LogWarning("blendshape doesn't exist: " + fixedName);
                return false;
            }

            if (!additive)
            {
                //if (weight != blendshapesDic[fixedName].weight)
                //{
                blendshapesDic[fixedName].weight = weight;
                return true;
                //}

                //return false;
            }
            else
            {
                blendshapesDic[fixedName].weight += weight;
                return true;
            }
        }

        public bool HasFac(string facName)
        {
            return facsDrivingBlendshapes.ContainsKey(facName);
        }

        public void ResetAllBlendshapeWeights()
        {
            foreach (Blendshape blendshape in blendshapes)
            {
                blendshape.weight = 0;
            }
        }

        /// <summary>
        /// Returns the weight of the specified blendshape. If the blendshape doesn't exit, returns -1.
        /// </summary>
        /// <param name="blendshapeName">The name of the blendshape.</param>
        /// <returns>Weight of the specified blendshape. -1 if blendshape isn't found.</returns>
        public float GetBlendshapeWeight(string blendshapeName)
        {
            if (blendshapesDic.ContainsKey(blendshapeName))
            {
                return blendshapesDic[blendshapeName].weight;
            }
            else
            {
                return -1;
            }
        }

        [Serializable]
        private class RigControlsSnapshot
        {
            [NonSerialized]
            public Transform targetBone;
            public string targetBoneName;

            public Vector3 vertexOffset;
            public Quaternion normalOffset;

            public Vector3 initialBonePos;
            public Quaternion initialBoneRotation;

            // rotation that converts from object space (the didimo model), into local space
            public Quaternion bindRotation;
        }


        [Serializable]
        public class Blendshape
        {
            [Serializable]
            public class InBetween
            {
                public InBetween(float finalWeight)
                {
                    this.finalWeight = finalWeight;
                    vertexOffsets = new List<Vector3>();
                    normalOffsets = new List<Quaternion>();
                }
                // the weight at which this blendshape has max influence (100%)
                public float finalWeight;

                public List<Vector3> vertexOffsets;
                public List<Quaternion> normalOffsets;
            }

            public float weight;
            public string name;
            // inBetweens MUST be in sorted by finalWeight, ascending order!
            public List<InBetween> inBetweens;

            public Vector3 GetVertexOffsetForCurrentWeight(int vertexId)
            {
                if (inBetweens.Count == 1 || weight < 0)
                {
                    return Vector3.Lerp(Vector3.zero, inBetweens[0].vertexOffsets[vertexId], weight);
                }

                // if weight bigger than 1, extrapolate from last two in-betweens
                if (weight >= 1.0)
                {
                    return Vector3.Lerp(inBetweens[inBetweens.Count - 2].vertexOffsets[vertexId], inBetweens[inBetweens.Count - 1].vertexOffsets[vertexId], weight);
                }

                for (int i = 0; i < inBetweens.Count; i++)
                {
                    if (inBetweens[i].finalWeight > weight)
                    {
                        if (i == 0)
                        {
                            float inBetweenWeight = weight / inBetweens[i].finalWeight;
                            return Vector3.Lerp(Vector3.zero, inBetweens[i].vertexOffsets[vertexId], inBetweenWeight);
                        }
                        else
                        {
                            float inBetweenWeight = (weight - inBetweens[i - 1].finalWeight) /
                                (inBetweens[i].finalWeight - inBetweens[i - 1].finalWeight);

                            return Vector3.Lerp(inBetweens[i - 1].vertexOffsets[vertexId], inBetweens[i].vertexOffsets[vertexId], inBetweenWeight);
                        }
                    }
                }

                Debug.LogError("Couldn't find in-between!");
                return Vector3.zero;
            }

            public Quaternion GetNormalOffsetForCurrentWeight(int vertexId)
            {
                if (inBetweens.Count == 1 || weight < 0)
                {
                    return Quaternion.Lerp(Quaternion.identity, inBetweens[0].normalOffsets[vertexId], weight);
                }

                // if weight bigger than 1, extrapolate from last two in-betweens
                if (weight >= 1)
                {
                    return Quaternion.Lerp(inBetweens[inBetweens.Count - 2].normalOffsets[vertexId],
                        inBetweens[inBetweens.Count - 1].normalOffsets[vertexId],
                        weight);
                }

                for (int i = 0; i < inBetweens.Count; i++)
                {
                    if (inBetweens[i].finalWeight > weight)
                    {
                        if (i == 0)
                        {
                            float inBetweenWeight = weight / inBetweens[i].finalWeight;
                            return Quaternion.Lerp(Quaternion.identity,
                                inBetweens[i].normalOffsets[vertexId],
                                inBetweenWeight);
                        }
                        else
                        {
                            float inBetweenWeight = (weight - inBetweens[i - 1].finalWeight) /
                                (inBetweens[i].finalWeight - inBetweens[i - 1].finalWeight);

                            return Quaternion.Lerp(inBetweens[i - 1].normalOffsets[vertexId],
                                inBetweens[i].normalOffsets[vertexId], inBetweenWeight);
                        }
                    }
                }

                Debug.LogError("Couldn't find in-between!");
                return Quaternion.identity;
            }
        }
    }
}
