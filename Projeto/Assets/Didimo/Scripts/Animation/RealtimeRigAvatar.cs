using System;
using System.Collections.Generic;
using Didimo.Animation.AnimationPlayer;
using UnityEngine;

namespace Didimo.Animation
{
    public class RealtimeRigAvatar : ScriptableObject
    {
        public void SortInBetweens()
        {
            foreach (Pose pose in poses)
            {
                pose.inBetweens.Sort((a, b) =>
                {
                    return a.finalWeight.CompareTo(b.finalWeight);
                });
            }

            // All in-btweens must have the same bones
            foreach (Pose pose in poses)
            {
                if (pose.inBetweens.Count > 1)
                {
                    foreach (Pose.Bone poseBone in pose.bones)
                    {
                        for (int i = 0; i < pose.inBetweens.Count; i++)
                        {
                            Pose.InBetween inBetween = pose.inBetweens[i];
                            if (inBetween.bones.Find(x => x.boneName == poseBone.targetName) == null)
                            {
                                Pose.InBetween.InBetweenBone inBetweenBone = new Pose.InBetween.InBetweenBone();
                                Pose.InBetween.InBetweenBone copyInBetweenBone = i > 0 ? pose.inBetweens[i - 1].bones.Find(x => x.boneName == poseBone.targetName) : null;

                                if (copyInBetweenBone == null)
                                {
                                    inBetweenBone.pos = Vector3.zero;
                                    inBetweenBone.rotq = Quaternion.identity;
                                }
                                else
                                {
                                    inBetweenBone.pos = copyInBetweenBone.pos;
                                    inBetweenBone.rotq = copyInBetweenBone.rotq;
                                }
                                inBetweenBone.boneName = poseBone.targetName;

                                pose.inBetweens[i].bones.Add(inBetweenBone);
                            }
                        }
                    }

                    // Sort In between bones
                    foreach (var inBetween in pose.inBetweens)
                    {
                        List<Pose.InBetween.InBetweenBone> inBetweensSorted = new List<Pose.InBetween.InBetweenBone>();
                        foreach (var poseBone in pose.bones)
                        {
                            inBetweensSorted.Add(inBetween.bones.Find(x => x.boneName == poseBone.targetName));
                        }
                        inBetween.bones = inBetweensSorted;
                    }
                }

                // Sanity check
                Debug.Assert(pose.bones.Count == pose.inBetweens[0].bones.Count, "Inbetweens bones and pose bones must match count");
                for (int i = 0; i < pose.inBetweens.Count - 1; i++)
                {
                    Debug.Assert(pose.inBetweens[i].bones.Count == pose.inBetweens[i + 1].bones.Count, "All inBetweens must have the same number of bones");

                    for (int j = 0; j < pose.inBetweens[i].bones.Count; j++)
                    {
                        Debug.Assert(pose.inBetweens[i].bones[j].boneName == pose.inBetweens[i + 1].bones[j].boneName, "All in betweens must have the same bone order");
                    }
                }
            }
        }

        public List<Pose> poses;
        public List<MeshDrivingBlendshapes> meshDrivingBlendshapes;

        [Serializable]
        public class Pose
        {
            public Vector3 GetPositionOffset(int boneIndex)
            {
                if (inBetweens.Count == 1 || weight < 0)
                {
                    return Vector3.Lerp(Vector3.zero, inBetweens[0].bones[boneIndex].pos, weight);
                }

                // if weight bigger than 1, extrapolate from last two in-betweens
                if (weight >= 1.0)
                {
                    return Vector3.Lerp(inBetweens[inBetweens.Count - 2].bones[boneIndex].pos, inBetweens[inBetweens.Count - 1].bones[boneIndex].pos, weight);
                }

                for (int i = 0; i < inBetweens.Count; i++)
                {
                    if (inBetweens[i].finalWeight > weight)
                    {
                        if (i == 0)
                        {
                            float inBetweenWeight = weight / inBetweens[i].finalWeight;
                            return Vector3.Lerp(Vector3.zero, inBetweens[i].bones[boneIndex].pos, inBetweenWeight);
                        }
                        else
                        {
                            float inBetweenWeight = (weight - inBetweens[i - 1].finalWeight) /
                                (inBetweens[i].finalWeight - inBetweens[i - 1].finalWeight);
                            float smoothWeight = Mathf.SmoothStep(inBetweens[i - 1].finalWeight, inBetweens[i].finalWeight, inBetweenWeight);

                            return Vector3.Lerp(inBetweens[i - 1].bones[boneIndex].pos, inBetweens[i].bones[boneIndex].pos, smoothWeight);
                        }
                    }
                }

                Debug.LogError("Couldn't find pose in-between!");
                return Vector3.zero;
            }

            public Quaternion GetRotationOffset(int boneIndex)
            {
                if (inBetweens.Count == 1 || weight < 0)
                {
                    return Quaternion.Lerp(Quaternion.identity, inBetweens[0].bones[boneIndex].rotq, weight);
                }

                // if weight bigger than 1, extrapolate from last two in-betweens
                if (weight >= 1)
                {
                    return Quaternion.Lerp(inBetweens[inBetweens.Count - 2].bones[boneIndex].rotq,
                        inBetweens[inBetweens.Count - 1].bones[boneIndex].rotq,
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
                                inBetweens[i].bones[boneIndex].rotq,
                                inBetweenWeight);
                        }
                        else
                        {

                            float inBetweenWeight = (weight - inBetweens[i - 1].finalWeight) /
                                (inBetweens[i].finalWeight - inBetweens[i - 1].finalWeight);
                            float smoothWeight = Mathf.SmoothStep(inBetweens[i - 1].finalWeight, inBetweens[i].finalWeight, inBetweenWeight);

                            return Quaternion.Lerp(inBetweens[i - 1].bones[boneIndex].rotq,
                                inBetweens[i].bones[boneIndex].rotq, smoothWeight);
                        }
                    }
                }

                Debug.LogError("Couldn't find pose in-between!");
                return Quaternion.identity;
            }

            [Serializable]
            public class InBetween
            {
                public InBetween(float finalWeight)
                {
                    this.finalWeight = finalWeight;
                }

                [Serializable]
                public class InBetweenBone
                {
                    public Vector3 pos;
                    public Quaternion rotq;
                    [NonSerialized]
                    public string boneName;
                }

                public List<InBetweenBone> bones;
                public float finalWeight;
            }

            [Serializable]
            public class Bone
            {
                public bool hasRot = false;
                public bool hasPos = false;

                public Vector3 initialPos;
                public Quaternion initialRotq;
                [NonSerialized]
                public Transform target;
                public string targetName;
            }

            [SerializeField]
            // The in betweens have a bone list, with the purpose of storing the position and rotation offsets (local space) 
            // Bone order should be the same as the bones list
            public List<InBetween> inBetweens;

            // This one's purpose is to store initial position and rotation
            // Order should be the same as the inBetweens bones list
            public List<Bone> bones;

            [NonSerialized]
            public float weight;
            public string name;
        }
    }
}