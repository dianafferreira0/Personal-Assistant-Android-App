using Didimo.Networking.Header;
using Didimo.Utils.Coroutines;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Didimo.Networking
{
    /// <summary>
    /// A custom yield instruction that will run a UnityWebRequest.
    /// Has support for cached requests that don't require a WWWForm.
    /// </summary>
    public class WWWCachedRequest : CustomYieldInstruction
    {
        protected static List<OngoingRequest> cachedOngoingRequests;

        /// <summary>
        /// Class used to track ongoing requests that are meant to be cached.
        /// When we call the same requests in quick succession, while the first hasn't finished yet, the following ones will wait until the first completes.
        /// After that, the requests after the first one will fetch the response from cache.
        /// </summary>
        protected class OngoingRequest
        {
            public string url;
            public object coroutine;
        }


        protected IEnumerator WaitForCachedOngoingRequest(string url, UnityAction action)
        {
            if (cachedOngoingRequests == null)
            {
                action();
                yield break;
            }

            foreach (OngoingRequest ongoingRequest in cachedOngoingRequests)
            {
                if (ongoingRequest.url.Equals(url))
                {
                    yield return ongoingRequest.coroutine;
                    cachedOngoingRequests.Remove(ongoingRequest);
                    break;
                }
            }
            action();
        }

        protected void AddCachedOngoingRequest(string url, object coroutine)
        {
            OngoingRequest ongoingRequest = new OngoingRequest();

            ongoingRequest.url = url;
            ongoingRequest.coroutine = coroutine;

            if (cachedOngoingRequests == null)
            {
                cachedOngoingRequests = new List<OngoingRequest>();
            }

            cachedOngoingRequests.Add(ongoingRequest);
        }

        protected void RemoveCachedOngoingRequest(string url, object coroutine)
        {
            foreach (OngoingRequest ongoingRequest in cachedOngoingRequests)
            {
                if (ongoingRequest.url.Equals(url))
                {
                    cachedOngoingRequests.Remove(ongoingRequest);
                    break;
                }
            }
        }

        public string Error
        {
            get
            {
                if (!string.IsNullOrEmpty(www.error))
                {
                    return www.error;
                }

                if (!www.url.StartsWith("file:") && !Cached && !(www.responseCode >= 200 && www.responseCode < 400))
                {
                    return "Error code " + www.responseCode.ToString();
                }

                string check_error_msg_rh = www.GetResponseHeader("Content-Type");
                if (check_error_msg_rh != null && check_error_msg_rh.Contains("application/json"))
                {
                    string json_txt = www.downloadHandler.text;
                    if (json_txt.Contains("\"stt\": \"NOK\""))
                    {
                        return "ERROR";
                    }
                }

                return null;
            }
        }
        public bool Cached { get; protected set; }
        public System.Func<UnityWebRequest, bool> ShouldCache;
        public bool IsDone { get; protected set; }
        protected System.Action<UnityWebRequest> doneDelegate;
        protected UnityWebRequest www;
        protected WWWForm form;
        protected byte[] binaryData;
        protected bool hasAuthentication;
        protected IRequestHeader requestHeaderManager;
        protected string url;
        protected string contentType;
        protected bool autoSaveCache;

        byte[] GetRequestData()
        {
            return form != null ? form.data : binaryData;
        }

        public string CachedFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }

                string path = GetCachePath(url, GetRequestData());

                if (File.Exists(path))
                {
                    return path;
                }

                return null;
            }
        }


        public override bool keepWaiting
        {
            get
            {
                return !IsDone;
            }
        }

        /// <summary>
        /// Create a request.
        /// </summary>
        /// <param name="coroutineManager">The coroutine manager.</param>
        /// <param name="requestHeader">The request header manager.</param>
        /// <param name="path">The request path.</param>
        /// <param name="cached">Should this request be cached?</param>
        /// <param name="doneDelegate">Delegate to be called when the request finishes.</param>
        /// <param name="downloadHandler">The download handler for this request.</param>
        /// <param name="hasAuthentication">Should this request send authentication headers? By default you should ignore this argument.</param>
        /// <param name="shouldCache">Determine, from the UnityWebRequest response given, if the request should be cached. Use it to discard unwanted answers, like error cases.</param>
        /// <param name="autoSaveCache">It true, the request will be saved automatically to cache once completed. Otherwise, the user must call SaveToCache manually.</param>
        public WWWCachedRequest(CoroutineManager coroutineManager,
            IRequestHeader requestHeader,
            string path,
            bool cached = false,
            System.Action<UnityWebRequest> doneDelegate = null,
            DownloadHandler downloadHandler = null,
            bool hasAuthentication = true,
            System.Func<UnityWebRequest, bool> shouldCache = null,
            bool autoSaveCache = true)
        {
            this.autoSaveCache = autoSaveCache;
            requestHeaderManager = requestHeader;
            this.doneDelegate = doneDelegate;
            Cached = cached;
            IsDone = false;
            this.hasAuthentication = hasAuthentication;
            this.ShouldCache = shouldCache;

            coroutineManager.StartCoroutine(WaitForFinalOperationsAsync(path, coroutineManager, null, downloadHandler));
        }

        /// <summary>
        /// Create a request.
        /// </summary>
        /// <param name="coroutineManager">The coroutine manager.</param>
        /// <param name="requestHeader">The request header manager.</param>
        /// <param name="path">The request path.</param>
        /// <param name="form">The form to submit.</param>
        /// <param name="cached">Should this request be cached?</param>
        /// <param name="doneDelegate">Delegate to be called when the request finishes.</param>
        /// <param name="uploadHandler">The upload handler for this request.</param>
        /// <param name="downloadHandler">The download handler for this request.</param>
        /// <param name="hasAuthentication">Should this request send authentication headers? By default you should ignore this argument.</param>
        /// <param name="shouldCache">Determine, from the UnityWebRequest response given, if the request should be cached. Use it to discard unwanted answers, like error cases.</param>
        /// <param name="autoSaveCache">It true, the request will be saved automatically to cache once completed. Otherwise, the user must call SaveToCache manually.</param>
        public WWWCachedRequest(CoroutineManager coroutineManager,
            IRequestHeader requestHeader,
            string path,
            WWWForm form,
            bool cached = false,
            System.Action<UnityWebRequest> doneDelegate = null,
            UploadHandler uploadHandler = null,
            DownloadHandler downloadHandler = null,
            bool hasAuthentication = true,
            System.Func<UnityWebRequest, bool> shouldCache = null,
            bool autoSaveCache = true)
        {
            this.autoSaveCache = autoSaveCache;
            requestHeaderManager = requestHeader;
            this.doneDelegate = doneDelegate;
            this.form = form;
            Cached = cached;
            IsDone = false;
            this.hasAuthentication = hasAuthentication;
            this.ShouldCache = shouldCache;

            coroutineManager.StartCoroutine(WaitForFinalOperationsAsync(path, coroutineManager, uploadHandler, downloadHandler));
        }

        /// <summary>
        /// Create a request.
        /// </summary>
        /// <param name="coroutineManager">The coroutine manager.</param>
        /// <param name="requestHeader">The request header manager.</param>
        /// <param name="path">The request path.</param>
        /// <param name="binaryData">The data to submit.</param>
        /// <param name="cached">Should this request be cached?</param>
        /// <param name="doneDelegate">Delegate to be called when the request finishes.</param>
        /// <param name="uploadHandler">The upload handler for this request.</param>
        /// <param name="downloadHandler">The download handler for this request.</param>
        /// <param name="hasAuthentication">Should this request send authentication headers? By default you should ignore this argument.</param>
        /// <param name="shouldCache">Determine, from the UnityWebRequest response given, if the request should be cached. Use it to discard unwanted answers, like error cases.</param>
        /// <param name="autoSaveCache">It true, the request will be saved automatically to cache once completed. Otherwise, the user must call SaveToCache manually.</param>
        /// <param name="contentType">The content type of the request. Passing null will use the default value (application/octet-stream). </param>
        public WWWCachedRequest(CoroutineManager coroutineManager,
            IRequestHeader requestHeader,
            string path,
            byte[] binaryData,
            bool cached = false,
            System.Action<UnityWebRequest> doneDelegate = null,
            UploadHandler uploadHandler = null,
            DownloadHandler downloadHandler = null,
            bool hasAuthentication = true,
            System.Func<UnityWebRequest, bool> shouldCache = null,
            bool autoSaveCache = true,
            string contentType = null
            )
        {
            this.autoSaveCache = autoSaveCache;
            requestHeaderManager = requestHeader;
            this.doneDelegate = doneDelegate;
            this.binaryData = binaryData;
            Cached = cached;
            IsDone = false;
            this.hasAuthentication = hasAuthentication;
            this.ShouldCache = shouldCache;
            this.contentType = contentType;

            coroutineManager.StartCoroutine(WaitForFinalOperationsAsync(path, coroutineManager, uploadHandler, downloadHandler));
        }

        protected virtual IEnumerator WaitForFinalOperationsAsync(string path, CoroutineManager coroutineManager, UploadHandler uploadHandler = null, DownloadHandler downloadHandler = null)
        {
            if (form != null && binaryData != null)
            {
                Debug.LogError("CachedRequestError - the request can have only either a WWWForm, or a byte[] defined as request data!");
                yield break;
            }

            // Unity stores and sets authentication cookies by default.
            // However, we are handling our own cookies. This will cause unity to add a cookie field to the header, and then we will also add another cookie field, making it duplicated.
            // If we are handling authentication, to fix this issue, we must clear the cache.
            //if (hasAuthentication)
            //{
            UnityWebRequest.ClearCookieCache(new System.Uri(ServicesRequestsConfiguration.DefaultConfig.baseDidimoUrl));
            //}

            if (Cached)
            {
                bool waiting = true;

                // Note: yielding this StartCoroutine call doesn't work... so we will use a "waiting" flag
                coroutineManager.StartCoroutine(WaitForCachedOngoingRequest(path, () => { waiting = false; }));

#if !UNITY_EDITOR
                while (waiting) yield return new WaitForEndOfFrame();
#else
                while (waiting) yield return new WaitForSecondsRealtime(.1f);
#endif
                AddCachedOngoingRequest(path, this);
            }

            if (Cached && IsCachedOrLocalFile(path, GetRequestData()))
            {
                www = new UnityWebRequest("file://" + GetCachePath(path, GetRequestData()));
            }
            else
            {
                if (form != null)
                {
                    www = UnityWebRequest.Post(path, form);
                }
                else if (binaryData != null)
                {
                    www = UnityWebRequest.Put(path, binaryData);
                    www.method = "POST";
                }
                else
                {
                    www = new UnityWebRequest(path);
                }

                if (!string.IsNullOrEmpty(contentType))
                {
                    www.SetRequestHeader("Content-Type", contentType);
                }

                foreach (KeyValuePair<string, string> headerEntry in ServicesRequestsConfiguration.DefaultConfig.AdditionalHeaders)
                {
                    www.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                }

#if !UNITY_WEBGL
                if (hasAuthentication)
                {
                    requestHeaderManager.UpdateForRequestUri(form, new System.Uri(path));

                    Dictionary<string, string> header = requestHeaderManager.GetHeader();
                    foreach (KeyValuePair<string, string> kvp in header)
                    {
                        www.SetRequestHeader(kvp.Key, kvp.Value);
                    }
                }
#endif
            }

            www.timeout = ServicesRequests.timeout; //timeout for requests; unity's default value (0) is limitless 

            if (downloadHandler != null)
            {
                www.downloadHandler = downloadHandler;
            }
            else
            {
                www.downloadHandler = new DownloadHandlerBuffer();
            }

            if (uploadHandler != null)
            {
                www.uploadHandler = uploadHandler;
            }

#if UNITY_2017_3_OR_NEWER
            yield return www.SendWebRequest();
#else
            yield return www.Send();
#endif

            url = www.url;

            if (Cached && ShouldCache != null)
            {
                Cached = ShouldCache(www);
            }

            if (Error != null)
            {
                Debug.LogWarning("Error during UnityWebRequest process with error code " + www.responseCode + ". \nURL: "+ www.url + "\n Error message: " + www.error);
            }
            else if (autoSaveCache && Cached && !IsCachedOrLocalFile(url, GetRequestData()))
            {
                SaveToCache(url, GetRequestData());
            }

            //if (hasAuthentication)
            //{
            requestHeaderManager.UpdateForResponseHeader(form, www.GetResponseHeaders());
            //}

            if (doneDelegate != null)
            {
                doneDelegate(www);
            }

            IsDone = true;
        }

        public void SaveToCache()
        {
            SaveToCache(url, GetRequestData());
        }

        protected void SaveToCache(string url, byte[] data)
        {
            string path = GetCachePath(url, data);
            //Debug.Log("Saving to cache path " + path);
            DirectoryInfo pathFolder = Directory.GetParent(path);
            Directory.CreateDirectory(pathFolder.FullName);
            File.WriteAllBytes(path, www.downloadHandler.data);

        }

        public void DeleteCache()
        {
            DeleteCache(url, GetRequestData());
        }

        void DeleteCache(string url, byte[] data)
        {
            string path = GetCachePath(url, data);
            File.Delete(path);
        }

        /// <summary>
        /// Returns true if there is a cache file for the given url.
        /// </summary>
        /// <param name="url">The url we want to check if is cached.</param>
        /// <returns>True if there is a cache file, false otherwise.</returns>
        public static bool IsCachedOrLocalFile(string url, byte[] data)
        {
            return url.StartsWith("file:") || File.Exists(GetCachePath(url, data));
        }

        /// <summary>
        /// Get the cache path for the given url.
        /// </summary>
        /// <param name="url">The url of the request.</param>
        /// <returns>The path to the cache file of the request.</returns>
        public static string GetCachePath(string url, byte[] data)
        {
            string full_url;

            if (url.Contains("?"))
            {
                string[] split = url.Split("?".ToCharArray(), 2);
                string urlPart = split[0];
                string queryPart = split[1];

                int queryHash = queryPart.GetHashCode();
                full_url = urlPart + queryHash;
            }
            else
            {
                full_url = url;
            }

            if (data != null)
            {
                full_url += GetHashCode(data).ToString();
                //full_url += requestHash;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(full_url);

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                sb.Replace(c, '_');
            }

            sb.Replace("/", "");
            sb.Replace(":", "_");

            string cachepath = Application.temporaryCachePath + "/WWWCache/" + sb;

            //to avoid PathTooLongException
            if (cachepath.Length > 250)
            {
                //Debug.LogWarning("Cache path too big. Using hash code of path \"" + sb + "\"");
                return Application.temporaryCachePath + "/WWWCache/" + sb.ToString().GetHashCode().ToString();
            }

            return cachepath;
        }

        /// <summary>
        /// Get the response of this request as text.
        /// </summary>
        /// <returns>The response string.</returns>
        public string GetText()
        {
            return www.downloadHandler.text;
        }

        /// <summary>
        /// Get the response of this request as Texture2D.
        /// </summary>
        /// <returns>The response Texture2D.</returns>
        public Texture2D GetTexture()
        {
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(www.downloadHandler.data))
            {
                return texture;
            }
            else
            {
                DeleteCache(url, form.data);
                return null;
            }
        }

        /// <summary>
        /// Get the response of this request as a byte array.
        /// </summary>
        /// <returns>The response byte array.</returns>
        public byte[] GetBytes()
        {
            return www.downloadHandler.data;
        }

        public static int GetHashCode(byte[] data)
        {

            if (data == null)
            {
                return 0;
            }

            int i = data.Length;
            int hc = i + 1;

            while (--i >= 0)
            {
                hc *= 257;
                hc ^= data[i];
            }

            return hc;
        }

        public DownloadHandler GetDownloadHandler()
        {
            return www.downloadHandler;
        }
    }
}