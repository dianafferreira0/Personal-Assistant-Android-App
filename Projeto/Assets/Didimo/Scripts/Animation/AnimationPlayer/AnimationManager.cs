using Didimo.Utils.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using Didimo.Menu;
using UnityEngine.Networking;
using System.Linq;

namespace Didimo.Animation.AnimationPlayer
{
    public partial class AnimationManager : MonoBehaviour
    {
        public AnimationPlayer animPlayer = null;
        public AudioSource audioSource;
        public float AUDIO_DELAY = .2f;

        private string currentBlendshapesAnimationText = "";
        //expressions poses
        public string expressionsJson = "";
        public DidimoAnimation expressionsTrack;
        //mocap animations
        public string[] mocapDefaultAnimationPaths;

        public int lastPlayedRecordingIndex = -1;

        public bool IsPlaying()
        {
            if (animPlayer != null)
                return animPlayer.IsPlaying;
            else return false;
        }
        public void PauseAnimation()
        {
            if (animPlayer != null)
                animPlayer.PauseAnimations(true);
            if (audioSource != null)
                audioSource.Pause();
        }

        #region Expressions Player

        public void PlayExpression_Neutral()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Neutral");
        }
        public void PlayExpression_Happy()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Happy");
        }
        public void PlayExpression_Sad()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Sad");
        }
        public void PlayExpression_Surprise()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Surprise");
        }
        public void PlayExpression_Anger()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Anger");
        }
        public void PlayExpression_Fear()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Fear");
        }
        public void PlayExpression_Disgust()
        {
            if (animPlayer != null)
                animPlayer.CrossFadeAnimation("Disgust");
        }

        #endregion


        public void Refresh()
        {
            if (animPlayer != null)
            {
                RemoveAllAnimations();
                LoadExpressionsAnimationTracks();
                CreateDefaultAnimationTracksFromCurrentPathList();
                CreateNewAnimationTracks();
                lastPlayedRecordingIndex = -1;
            }
        }

        public void RemoveAllAnimations()
        {
            if (animPlayer != null)
                animPlayer.RemoveAllAnimations();
        }


        public void ClearAnimPlayer()
        {
            animPlayer = null;
        }

        public void LoadExpressionsAnimationTracks()
        {
            DidimoAnimation da_neutral = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Neutral") as DidimoAnimation);
            DidimoAnimation da_happy = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Happy") as DidimoAnimation);
            DidimoAnimation da_sad = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Sad") as DidimoAnimation);
            DidimoAnimation da_surprise = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Surprise") as DidimoAnimation);
            DidimoAnimation da_anger = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Anger") as DidimoAnimation);
            DidimoAnimation da_fear = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Fear") as DidimoAnimation);
            DidimoAnimation da_disgust = (Resources.Load("DidimoImporter/2.0/AnimationFiles/DefaultExpressions/Disgust") as DidimoAnimation);

            da_neutral.name = da_neutral.name.Replace("(Clone)", "");
            da_happy.name = da_happy.name.Replace("(Clone)", "");
            da_sad.name = da_sad.name.Replace("(Clone)", "");
            da_surprise.name = da_surprise.name.Replace("(Clone)", "");
            da_anger.name = da_anger.name.Replace("(Clone)", "");
            da_fear.name = da_fear.name.Replace("(Clone)", "");
            da_disgust.name = da_disgust.name.Replace("(Clone)", "");

            animPlayer.AddAnimationTrack(da_neutral);
            animPlayer.AddAnimationTrack(da_happy);
            animPlayer.AddAnimationTrack(da_sad);
            animPlayer.AddAnimationTrack(da_surprise);
            animPlayer.AddAnimationTrack(da_anger);
            animPlayer.AddAnimationTrack(da_fear);
            animPlayer.AddAnimationTrack(da_disgust);
        }

        //to store the configuration and not need to reload 
        public void SetDefaultAnimationTracksFromPathList(List<string> animationPathList)
        {
            mocapDefaultAnimationPaths = new string[animationPathList.Count];
            for (int i = 0; i < animationPathList.Count; i++)
            {
                mocapDefaultAnimationPaths[i] = animationPathList[i];
            }
        }
        //use the stored default configuration when reloading 
        public void CreateDefaultAnimationTracksFromCurrentPathList()
        {
            for (int i = 0; i < mocapDefaultAnimationPaths.Length; i++)
            {
                DidimoAnimation da = (Resources.Load(mocapDefaultAnimationPaths[i]) as DidimoAnimation);
                da.animationName = "default_" + i.ToString();
                animPlayer.AddAnimationTrack(da);
            }
        }

        public void RePlayAnimationAtLastIndex(Action animationEndDelegate, bool playFrameByFrame = false)
        {
            PlayAnimationAtIndex(lastPlayedRecordingIndex, animationEndDelegate, playFrameByFrame);
        }
        public void PlayAnimationAtIndex(int recording_index, Action animationEndDelegate, bool playFrameByFrame = false)
        {
            StartCoroutine(PlayAnimationAtIndexAsync(recording_index, animationEndDelegate, playFrameByFrame));
        }
        public IEnumerator PlayAnimationAtIndexAsync(int recording_index, Action animationEndDelegate, bool playFrameByFrame = false)
        {
#if UNITY_EDITOR
            string path = Application.dataPath;
#else
			string path = Application.persistentDataPath;
#endif
            DirectoryInfo info = new DirectoryInfo(path);
            FileInfo[] fileInfo = info.GetFiles("facecapture_*.txt").OrderByDescending(p => p.LastWriteTime).ToArray();

            string animationName = "";
            if (recording_index >= fileInfo.Length) //play default animation
            {
                int defaultMocapAnimationToPlay_index = recording_index - fileInfo.Length;
                animationName = "default_" + defaultMocapAnimationToPlay_index.ToString();
            }
            else
            {
                animationName = recording_index.ToString();
            }

            //save last played recording index
            lastPlayedRecordingIndex = recording_index;

            animPlayer.PlayAnimation(animationName, animationEndDelegate);
            animPlayer.PauseAnimations(playFrameByFrame);
            animPlayer.UpdatePose();
            //if audio is playing, then the position should be also reset --> original class code seems confusing, needs more inspection 
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.time = 0 - AUDIO_DELAY;

                //load audioclip if available
                if (recording_index >= fileInfo.Length)
                {
                    audioSource.clip = null;
                }
                else
                {
                    string audiofilename = fileInfo[recording_index].Name.Replace(".txt", ".wav");
                    FileInfo[] audiofileInfoArray = info.GetFiles(audiofilename);
                    if (audiofileInfoArray.Length == 1)
                    {
                        string audiofullpath = audiofileInfoArray[0].FullName;
                        AudioClip temp = null;
                        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + audiofullpath, AudioType.WAV))
                        {
                            yield return www.SendWebRequest();
                            if (www.isNetworkError || www.isHttpError)
                            {
                                Debug.Log(www.error);
                            }
                            else
                            {
                                temp = DownloadHandlerAudioClip.GetContent(www);
                            }
                        }
                        audioSource.clip = temp;
                        if (!playFrameByFrame && audioSource.clip != null)
                            audioSource.Play();
                    }
                    else audioSource.clip = null;
                }
            }
        }


        IEnumerator LoadBlendshapesDelayed(AnimationPlayer animPlayer, string url)
        {
            using (var www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log("Error - LoadBlendshapesDelayed - " + www.error);
                }
                else
                {
                    Debug.Log("LoadBlendshapesDelayed - Content: \n" + www.downloadHandler.text);
                    currentBlendshapesAnimationText = www.downloadHandler.text;
                }
            }
        }

        IEnumerator LoadAudioDelayed(AudioSource audioSource, string url)
        {
            using (var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {
                yield return www;

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log("Error - LoadAudioDelayed - " + www.error);
                    // for example, often 'Error .. 404 Not Found'
                }
                else
                {
                    audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        public bool HasAnimationBeenLoaded()
        {
            return lastPlayedRecordingIndex != -1;
        }


        /// <summary>
        /// CreateNewAnimationTracks - Searches for mocap files in the filesystem and adds as items to the animation player
        /// </summary>
        public void CreateNewAnimationTracks()
        {
            try
            {
#if UNITY_EDITOR
                string path = Application.dataPath;
#else
			string path = Application.persistentDataPath;
#endif

                DirectoryInfo info = new DirectoryInfo(path);
                FileInfo[] fileInfo = info.GetFiles("facecapture_*.txt").OrderByDescending(p => p.LastWriteTime).ToArray();

                for (int i = 0; i < fileInfo.Length; i++)
                {
                    FileInfo file = fileInfo[i];
                    string textContent = "";

                    using (StreamReader textReader = new StreamReader(fileInfo[i].FullName))
                    {
                        //read file
                        textContent = textReader.ReadToEnd();
                        //cleanup
                        textReader.DiscardBufferedData();
                        textReader.BaseStream.Seek(0, SeekOrigin.Begin);

                    }

                    DidimoAnimation da = DidimoAnimation.CreateInstanceFromJsonString(i.ToString(), textContent);
                    Debug.Log("CreateNewAnimationTracks - Added mocap Animation name: " + da.animationName);
                    da.wrapMode = WrapMode.Once;
                    animPlayer.AddAnimationTrack(da);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }


    }
}
