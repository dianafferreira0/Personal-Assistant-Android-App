using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Didimo.Utils.Serialization.SimpleJSON;

namespace Didimo.Animation.AnimationPlayer
{
    public class DidimoAnimation : ScriptableObject
    {
        // Unity can't serialize nested lists, so we need this hack
        [Serializable]
        public class FloatList
        {
            [SerializeField]
            public List<float> list;

            public FloatList(List<float> values)
            {
                list = new List<float>(values);
            }

            public FloatList()
            {
                list = new List<float>();
            }
            public FloatList(int capacity)
            {
                list = new List<float>(capacity);
            }

            public float this[int index]    // Indexer declaration  
            {
                get
                {
                    return list[index];
                }
                set
                {
                    list[index] = value;
                }
            }

            public void Add(float value)
            {
                list.Add(value);
            }
        }

        public enum Status
        {
            STOPPED,
            PLAYING,
            FADE_IN,
            FADE_OUT
        }

        private Status status = Status.STOPPED;

        public float speed = 1.0f;
        public string animationName;
        [SerializeField]
        protected int numberOfFrames;
        public int fps;

        public List<string> facNames;
        [SerializeField]
        public List<FloatList> facValues;

        [NonSerialized]
        public float animationTime = 0;
        [SerializeField]
        protected float totalAnimationTime = 0;
        public WrapMode wrapMode;
        public Action animationReachedEndAction;
        private float crossFadeDuration = 0.3f;
        private float weight = 0.0f;

        public void FadeIn(float duration = 0.3f)
        {
            status = Status.FADE_IN;
            crossFadeDuration = duration;
        }

        public void FadeOut(float duration = 0.3f)
        {
            status = Status.FADE_OUT;
            crossFadeDuration = duration;
        }

        public void Play()
        {
            weight = 1f;
            animationTime = 0;
            status = Status.PLAYING;
        }

        public void Stop()
        {
            weight = 0f;
            animationTime = 0;
            status = Status.STOPPED;
        }

        public bool IsStopped
        {
            get
            {
                return status == Status.STOPPED;
            }
        }

        public int GetNumberOfFrames()
        {
            return numberOfFrames;
        }

        public void Tick(float deltaTime)
        {
            switch (status)
            {
                case Status.STOPPED:
                    return;
                case Status.FADE_IN:
                    weight += deltaTime / crossFadeDuration;
                    if (weight >= 1.0f)
                    {
                        weight = 1.0f;
                        status = Status.PLAYING;
                    }
                    break;
                case Status.FADE_OUT:
                    weight -= deltaTime / crossFadeDuration;
                    if (weight <= 0.0f)
                    {
                        weight = 0.0f;
                        animationTime = 0;
                        status = Status.STOPPED;
                        return;
                    }
                    break;
                case Status.PLAYING:
                    break;
            }

            animationTime += deltaTime * speed;

            if (animationTime > totalAnimationTime || animationTime < 0.0f)
            {
                switch (wrapMode)
                {
                    case WrapMode.ClampForever:
                        animationTime = totalAnimationTime;
                        break;
                    case WrapMode.Loop:
                        animationTime = 0;
                        break;
                    case WrapMode.Once:
                        animationTime = 0;
                        status = Status.STOPPED;
                        break;
                    case WrapMode.PingPong:
                        animationTime = speed > 0 ? totalAnimationTime - (animationTime - totalAnimationTime) : Mathf.Abs(animationTime);
                        speed *= -1.0f;
                        break;
                    default:
                        Debug.LogError("Unsupported animation wrap mode: " + wrapMode);
                        break;

                }
                if (animationReachedEndAction != null)
                {
                    animationReachedEndAction.Invoke();
                    animationReachedEndAction = null;
                }
            }
        }

        public static DidimoAnimation CreateInstanceForConfig(string animationName, List<string> facNames, List<List<float>> facValues, int fps, WrapMode wrapMode)
        {
            DidimoAnimation result = (DidimoAnimation)CreateInstance(typeof(DidimoAnimation));

            result.animationName = animationName;
            result.facNames = new List<string>(facNames);

            result.facValues = new List<FloatList>();

            int valuesCount = facValues[0].Count;
            foreach (List<float> values in facValues)
            {
                Debug.Assert(values.Count == valuesCount);
                result.facValues.Add(new FloatList(values));
            }

            result.fps = fps;
            result.numberOfFrames = valuesCount;
            result.totalAnimationTime = (float)result.numberOfFrames / result.fps;
            result.wrapMode = wrapMode;

            return result;
        }

        public static DidimoAnimation CreateInstanceFromJsonString(string animationName, string animationJsonStr)
        {
            DidimoAnimation result = (DidimoAnimation)CreateInstance(typeof(DidimoAnimation));

            result.animationName = animationName;
            JSONNode animationJson = JSON.Parse(animationJsonStr);

            JSONNode ctrl = animationJson["ctrl_m_FACS_001"];
            if (ctrl != null)
            {
                animationJson = ctrl;
            }

            result.fps = 60;
            result.numberOfFrames = animationJson[0].AsArray.Count;
            result.facNames = new List<string>();
            result.facValues = new List<FloatList>();

            foreach (string facName in animationJson.Keys)
            {
                var frames = animationJson[facName];

                Debug.Assert(result.numberOfFrames == frames.Count);

                FloatList values = new FloatList(result.numberOfFrames);
                for (int i = 0; i < frames.Count; i++)
                {
                    values.Add(frames[i]);
                }

                result.facValues.Add(values);
                result.facNames.Add(facName);
            }

            result.totalAnimationTime = (float)result.numberOfFrames / result.fps;
            return result;
        }

        public float NormalizedTime
        {
            get
            {
                return animationTime / totalAnimationTime;
            }

            set
            {
                animationTime = value * totalAnimationTime;
            }
        }

        public void GetAnimationFacValues(ref List<float> currentFacValues, out List<string> facNames)
        {
            facNames = this.facNames;

            currentFacValues = new List<float>(facNames.Count);

            int currentFrame = (int)(NormalizedTime * (numberOfFrames - 1));
            if (currentFrame < 0 || currentFrame >= numberOfFrames)
            {
                Debug.LogError("Invalid frame index: " + currentFrame);
            }
            else
            if (currentFrame == numberOfFrames - 1)
            {
                for (int i = 0; i < facNames.Count; i++)
                {
                    currentFacValues.Add(facValues[i][currentFrame] * weight);
                }
            }
            else
            {
                float weight1 = (NormalizedTime * (numberOfFrames - 1)) - currentFrame;
                float weight0 = 1.0f - weight1;

                for (int i = 0; i < facValues.Count; i++)
                {
                    currentFacValues.Add((facValues[i][currentFrame] * weight0 + facValues[i][currentFrame + 1] * weight1) * weight);
                }
            }
        }

        public void GetAnimationFacValuesForFrame(int frameIndex, ref List<float> currentFacValues, out List<string> facNames)
        {
            facNames = this.facNames;
            currentFacValues = new List<float>(facNames.Count);

            if (frameIndex < 0 || frameIndex >= numberOfFrames)
            {
                Debug.LogError("Invalid frame index: " + frameIndex);
            }
            else
            {
                for (int i = 0; i < facNames.Count; i++)
                {
                    currentFacValues.Add(facValues[i][frameIndex] * weight);
                }
            }
        }
    }
}