using Didimo.Animation.AnimationPlayer;
using Didimo.Animation.DataObjects;
using Didimo.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Didimo.Animation
{
    [Serializable]
    public class RealtimeRig : MonoBehaviour
    {
        private bool shouldUpdateBonePositions = false;
        private bool shouldUpdateBlendshapeWeights = false;
        private List<string> facNames;
        // for debug
        private Dictionary<string, float> facWeights;
        public float GetWeightForFAC(string fac)
        {
            return facWeights[fac];
        }

        public RealtimeRigAvatar realtimeRigAvatar;

        private void Awake()
        {
            if (realtimeRigAvatar != null)
            {
                Init();
            }
        }

        public void Init()
        {
            facWeights = new Dictionary<string, float>();
            foreach (RealtimeRigAvatar.Pose pose in realtimeRigAvatar.poses)
            {
                // In betweens should all have the same bones
                foreach (RealtimeRigAvatar.Pose.Bone bone in pose.bones)
                {
                    bone.target = transform.FindRecursive(bone.targetName);
                }
            }

            if (realtimeRigAvatar.meshDrivingBlendshapes != null)
            {
                foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
                {
                    mdb.Init(transform);

                    List<string> mdbFacNames = mdb.GetFacNames();
                    foreach (string facName in mdbFacNames)
                    {
                        facWeights[facName] = 0;
                    }
                }
            }

            facNames = new List<string>();
            if (facWeights != null)
            {
                foreach (var facWeight in facWeights)
                {
                    facNames.Add(facWeight.Key);
                }
            }

        }

        // Also sets pose weight
        public bool SetBlendshapeWeightsForFac(string facName, float weight, bool additive = false)
        {
            if (additive && facWeights.ContainsKey(facName))
            {
                facWeights[facName] += weight;
            }
            else
            {
                facWeights[facName] = weight;
            }

            bool hasFacName = false;
            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                if (mdb.HasFac(facName))
                {
                    shouldUpdateBlendshapeWeights = true;
                    hasFacName = true;
                }
            }

            foreach (RealtimeRigAvatar.Pose pose in realtimeRigAvatar.poses)
            {
                if (pose.name == facName)
                {
                    if (additive)
                    {
                        pose.weight += weight;

                    }
                    else
                    {
                        pose.weight = weight;
                    }

                    hasFacName = true;
                    shouldUpdateBlendshapeWeights = true;
                }
            }

            return hasFacName;
        }

        public List<string> GetSupportedFACS()
        {
            return facNames;
        }

        public List<string> GetBlendshapeNames()
        {
            List<string> blendshapeNames = new List<string>();
            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                blendshapeNames.AddRange(mdb.GetBlendshapeNames());
            }
            return blendshapeNames;
        }

        /// <summary>
        /// Searches all MeshDrivingBlendshapes, and returns the first found blendshape with the specified name. -1 if no blendshape is found.
        /// </summary>
        /// <param name="blendshapeName">The name of the blendshape.</param>
        /// <returns>Weight of the specified blendshape. -1 if blendshape isn't found.</returns>
        public float GetBlendshapeWeight(string blendshapeName)
        {
            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                float weight = mdb.GetBlendshapeWeight(blendshapeName);
                if (weight != -1)
                {
                    return weight;
                }
            }

            return -1;
        }

        /// <summary>
        /// Reset all blendshapes.
        /// </summary>
        /// <param name="forceReset">If forceReset is true, will always reset. If forceReset is false, will only reset up to once per frame.</param>
        public void ResetAll(bool forceReset = false)
        {
            if ((shouldUpdateBonePositions || shouldUpdateBlendshapeWeights) && !forceReset)
            {
                return;
            }

            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                mdb.ResetAllBlendshapeWeights();
            }

            foreach (RealtimeRigAvatar.Pose pose in realtimeRigAvatar.poses)
            {
                pose.weight = 0;
            }

            foreach (var key in facNames)
            {
                facWeights[key] = 0;
            }
            shouldUpdateBlendshapeWeights = true;
            shouldUpdateBonePositions = true;
        }

        public void SetBlendshapeWeight(string blendshapeName, float weight, bool additive)
        {
            bool changed = false;
            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                changed |= mdb.SetBlendshapeWeight(blendshapeName, weight, additive);
            }

            if (changed)
            {
                shouldUpdateBonePositions = true;
            }
            else
            {
                Debug.LogWarning("blendshape doesn't exist: " + blendshapeName);
            }
        }


        /// <summary>
        /// Build the realtime rig, given an input configuration. This configuration part of the output of the didimo pipeline, which you can download through the API.
        /// </summary>
        /// <param name="realtimeRigDataObject">The realtime rig data object, from which we will create the realtime rig avatar (configuration).</param>
        /// <param name="instantiateNewAvatar">RealtimeRigAvatars can be shared between multiple didimo instances. If we don't want to update all those instances, pass true to this parameter.</param>
        public void Build(RealtimeRigDataObject realtimeRigDataObject, bool instantiateNewAvatar = false)
        {
            if (instantiateNewAvatar)
            {
                realtimeRigAvatar = Instantiate(realtimeRigAvatar);
            }

            realtimeRigAvatar.meshDrivingBlendshapes = new List<MeshDrivingBlendshapes>();

            if(realtimeRigDataObject.blendshaped_meshes != null)
            {
                foreach (KeyValuePair<string, RealtimeRigDataObject.BlendShapedMeshDataObject> blendshaped_mesh in realtimeRigDataObject.blendshaped_meshes)
                {
                    MeshDrivingBlendshapes meshDrivingBlendshape = new MeshDrivingBlendshapes(blendshaped_mesh.Value, transform.FindRecursive(blendshaped_mesh.Key), transform, realtimeRigDataObject.unitsPerMeter);
                    realtimeRigAvatar.meshDrivingBlendshapes.Add(meshDrivingBlendshape);
                }
            }
            

            realtimeRigAvatar.poses = new List<RealtimeRigAvatar.Pose>();

            if(realtimeRigDataObject.poses != null)
            {
                foreach (KeyValuePair<string, Dictionary<string, RealtimeRigDataObject.BoneOffsets>> dataObjectPose in realtimeRigDataObject.poses)
                {
	                // Get the pose name, without the "_w###" final part, which indicates the max weight of the in-between
	                string poseName = Regex.Replace(dataObjectPose.Key, "_w[0-9][0-9][0-9]$", "");
	                // if we can match with "_w###", at the end of the name, match with the ### part (weight between 0 and 100), and convert it to a float between 0.0 and 1.0
	                Match match = Regex.Match(dataObjectPose.Key, "(?<=_w)[0-9][0-9][0-9]$");
	                float inBetweenMaxWeight;
	                if (match.Success)
	                {
	                    inBetweenMaxWeight = float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) / 100.0f;
	                }
	                else
	                {
	                    inBetweenMaxWeight = 1.0f;
	                }

	                RealtimeRigAvatar.Pose pose = realtimeRigAvatar.poses.Find(p => p.name == poseName);
	                if (pose == null)
	                {
	                    pose = new RealtimeRigAvatar.Pose();
	                    pose.name = poseName;
	                    pose.inBetweens = new List<RealtimeRigAvatar.Pose.InBetween>();
	                    realtimeRigAvatar.poses.Add(pose);
	                    pose.bones = new List<RealtimeRigAvatar.Pose.Bone>();
	                }

	                RealtimeRigAvatar.Pose.InBetween inBetween = new RealtimeRigAvatar.Pose.InBetween(inBetweenMaxWeight);
	                pose.inBetweens.Add(inBetween);
	                inBetween.bones = new List<RealtimeRigAvatar.Pose.InBetween.InBetweenBone>();
	                pose.name = poseName;

	                foreach (KeyValuePair<string, RealtimeRigDataObject.BoneOffsets> dataObjectBone in dataObjectPose.Value)
	                {
	                    RealtimeRigAvatar.Pose.Bone bone = pose.bones.Find(x => x.targetName == dataObjectBone.Key);
	                    if (bone == null)
	                    {
	                        bone = new RealtimeRigAvatar.Pose.Bone();
	                        bone.targetName = dataObjectBone.Key;
	                        pose.bones.Add(bone);
	                    }

	                    RealtimeRigAvatar.Pose.InBetween.InBetweenBone inBetweenBone = new RealtimeRigAvatar.Pose.InBetween.InBetweenBone();
	                    inBetweenBone.boneName = dataObjectBone.Key;

	                    Transform boneTransform = transform.FindRecursive(dataObjectBone.Key);

	                    if (boneTransform == null)
	                    {
	                        Debug.LogError("Failed to find bone named '" + dataObjectBone.Key + "'");
	                        continue;
	                    }

	                    if (dataObjectBone.Value.pos != null)
	                    {
	                        inBetweenBone.pos = new Vector3(-dataObjectBone.Value.pos[0], dataObjectBone.Value.pos[1], dataObjectBone.Value.pos[2]) / realtimeRigDataObject.unitsPerMeter;
	                        bone.hasPos = true;
	                        bone.initialPos = boneTransform.localPosition;
	                    }

	                    if (dataObjectBone.Value.rotq != null)
	                    {
	                        inBetweenBone.rotq = new Quaternion(dataObjectBone.Value.rotq[0], -dataObjectBone.Value.rotq[1], -dataObjectBone.Value.rotq[2], dataObjectBone.Value.rotq[3]);
	                        bone.hasRot = true;
	                        bone.initialRotq = boneTransform.localRotation;
	                    }
	                    else
	                    {
	                        // Make sure the rotation always has a value that makes sense
	                        bone.initialRotq = Quaternion.identity;
	                    }

	                    inBetween.bones.Add(inBetweenBone);
	                }
                }
            }
            


            // InBetweens need to be sorted
            realtimeRigAvatar.SortInBetweens();

            if (Application.isPlaying)
            {
                Init();
            }
        }

        // Update blendshape weights according to active FACS
        void UpdateBlendshapeWeights()
        {
            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                mdb.UpdateBlendshapeWeights(facWeights);
            }
        }

        public void UpdateRigDeformation()
        {
            if (shouldUpdateBlendshapeWeights)
            {
                UpdateBlendshapeWeights();
                shouldUpdateBlendshapeWeights = false;
                shouldUpdateBonePositions = true;
            }

            if (shouldUpdateBonePositions)
            {
                foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
                {
                    mdb.CalculateComposedBlendshape();
                }

                UpdateBonePositions();
                shouldUpdateBonePositions = false;
            }
        }

        // This will make sure RealtimeRig's Update runs after all other scripts.
        // Needed because multiple components may control the RealtimeRig, and because we use constraints, which get evaluated after Update and before LateUpdate.
        private System.Collections.IEnumerator DelayedUpdate()
        {
            yield return null;
            UpdateRigDeformation();
        }

        // Update is called once per frame
        public void Update()
        {
            StartCoroutine(DelayedUpdate());
        }


        public void UpdateBonePositions()
        {
            // Reset the bone transforms
            foreach (RealtimeRigAvatar.Pose pose in realtimeRigAvatar.poses)
            {
                foreach (RealtimeRigAvatar.Pose.Bone bone in pose.bones)
                {
                    if (bone.hasPos)
                    {
                        bone.target.localPosition = bone.initialPos;
                    }
                    if (bone.hasRot)
                    {
                        bone.target.localRotation = bone.initialRotq;
                    }
                }
            }

            foreach (MeshDrivingBlendshapes mdb in realtimeRigAvatar.meshDrivingBlendshapes)
            {
                mdb.UpdateBonePositions();
            }

            // Update the deformation on the bones through bone poses
            foreach (RealtimeRigAvatar.Pose pose in realtimeRigAvatar.poses)
            {
                if (pose.weight > 0)
                {
                    for (int i = 0; i < pose.bones.Count; i++)
                    {
                        if (pose.bones[i].hasRot)
                        {
                            pose.bones[i].target.localRotation *= pose.GetRotationOffset(i);
                        }
                        if (pose.bones[i].hasPos)
                        {
                            // Translation is already stored in the bone's local coordinates
                            pose.bones[i].target.localPosition += pose.GetPositionOffset(i);
                        }

                        if (!pose.bones[i].hasPos && !pose.bones[i].hasRot)
                        {
                            Debug.LogWarning("Bone " + pose.bones[i].targetName + " has no position nor rotation change");
                        }
                    }
                }
            }
        }
    }
}
