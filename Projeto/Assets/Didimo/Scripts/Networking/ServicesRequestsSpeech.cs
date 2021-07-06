using Didimo.Networking.DataObjects;
using Didimo.Networking.Header;
using Didimo.Speech;
using Didimo.Utils.Coroutines;
using Didimo.Utils.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Didimo.Networking
{

    public enum SPEECH_VOICES { Brian, Amy };


    /// <summary>
    /// Class that handles services requests to the Didimo API.
    /// </summary>
    public partial class ServicesRequests : ServicesRequestsBase
    {

        public object GetSpeechFiles(CoroutineManager coroutineManager, string text, SPEECH_VOICES voiceId, int vocal_tract_length, int pitch, System.Action<List<Viseme>, AudioClip> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(GetSpeechFilesAsync(coroutineManager, text, voiceId, vocal_tract_length, pitch, doneDelegate, onFailureDelegate));
        }

        IEnumerator GetSpeechFilesAsync(CoroutineManager coroutineManager, string text, SPEECH_VOICES voiceId, int vocal_tract_length, int pitch, System.Action<List<Viseme>, AudioClip> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWForm form = new WWWForm();
            string param_vocal_tract_length = vocal_tract_length < 0 ? "" + vocal_tract_length : "+" + vocal_tract_length;
            string param_pitch = pitch < 0 ? "" + pitch : "+" + pitch;
            string ssml_text = "<speak><amazon:effect vocal-tract-length='" + param_vocal_tract_length + "%'><prosody pitch='" + param_pitch + "%'>" + text + " </prosody></amazon:effect></speak>";

            form.AddField("text", ssml_text);
            string voice_to_play = "";
            /*if (VisemePlayer.SpeechVoice_param != null)
                voice_to_play = VisemePlayer.SpeechVoice_param;
            else*/ voice_to_play = voiceId.ToString();

            form.AddField("voice", voice_to_play);
            form.AddField("type", "ssml");

            WWWResponseRequest visemeRequest = new WWWResponseRequest(
             coroutineManager,
             requestHeader,
             ServicesRequestsConfiguration.DefaultConfig.speechUrl,
             form, true);

            yield return visemeRequest;

            if (visemeRequest.Error != null)
            {
                HandleError(visemeRequest, onFailureDelegate);
            }
            else
            {
                SpeechDataObject speechDataObject = visemeRequest.GetResponseJson<SpeechDataObject>();

                SessionRequestHeader sessionRequestHeader = new SessionRequestHeader();
                sessionRequestHeader.SessionID = sessionRequestId;
                IRequestHeader extRequestHeader = sessionRequestHeader;

                WWWCachedRequest marksRequest = new WWWCachedRequest(coroutineManager, extRequestHeader, speechDataObject.marksURL.Replace("\\", ""), true);
                string audioFileUrl = speechDataObject.audioURL.Replace("\\", "");
                WWWCachedRequest audioRequest = new WWWCachedRequest(coroutineManager, extRequestHeader, audioFileUrl, true, null, new DownloadHandlerAudioClip(audioFileUrl, AudioType.OGGVORBIS));

                yield return marksRequest;
                yield return audioRequest;

                if (!string.IsNullOrEmpty(marksRequest.Error))
                {
                    Debug.LogError(marksRequest.Error);
                    onFailureDelegate(new System.Exception(marksRequest.Error));
                    yield break;
                }

                if (!string.IsNullOrEmpty(audioRequest.Error))
                {
                    Debug.LogError(audioRequest.Error);
                    onFailureDelegate(new System.Exception(audioRequest.Error));
                    yield break;
                }

                AudioClip clip = ((DownloadHandlerAudioClip)audioRequest.GetDownloadHandler()).audioClip;

                // Transform the file into json format... In a pretty nasty way
                string marksJson = marksRequest.GetText();
                marksJson = marksJson.Insert(0, "[");
                marksJson += "]";
                marksJson = marksJson.Replace("}", "},");
                marksJson = marksJson.Replace("},]", "}]");

                Debug.Log(marksJson);

                List<Viseme> allVisemes = MiniJSON.Deserialize<List<Viseme>>(marksJson) as List<Viseme>;

                doneDelegate(allVisemes, clip);
            }
        }
    }
}