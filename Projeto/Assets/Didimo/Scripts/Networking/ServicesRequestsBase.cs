using Didimo.Menu;
using Didimo.Networking.DataObjects;
using Didimo.Networking.Header;
using Didimo.Utils;
using Didimo.Utils.Coroutines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Didimo.Networking
{
    public enum PreviewType
    {
        FrontPerspective = 0
    }

    public enum InputType
    {
        Photo = 0,
        LoFiMesh = 1,
        HiFiMesh = 2
    }


    /// <summary>
    /// Class that handles services requests to the Didimo API.
    /// </summary>
    public partial class ServicesRequestsBase
    {
        public static int timeout = 0; //timeout for requests; unity's default value (0) is limitless 
        public static int downloads_chunk_size = 200000; //for tracking download progress
        public static int MAX_PHOTO_PIXELS = 8388608; // our api has a maximum number pixels for photos

        public static string scaling_error_message = "The selected photo exceeds the maximum size. Please select another photo and try to upload again";

        protected const float checkAvatarGenerationProgressIntervalInSeconds = 6.0f;
        protected const float checkAssetGenerationProgressIntervalInSeconds = 2.0f;

        private static ServicesRequests gameInstance;
        private static ServicesRequests editorInstance;

        public IRequestHeader requestHeader;
        protected string sessionRequestId;

        protected ServicesRequestsBase() { }

        public void ConfigureForSessionHeader()
        {
            SessionRequestHeader sessionRequestHeader = new SessionRequestHeader();
            sessionRequestHeader.SessionID = sessionRequestId;
            requestHeader = sessionRequestHeader;
        }

        public void ConfigureForAPIKeyHeader(string apiKey, string secretKey)
        {
            requestHeader = new APIKeyRequestHeader(apiKey, secretKey);
        }

        public static ServicesRequests GameInstance
        {
            get
            {
                if (gameInstance == null)
                {
                    gameInstance = new ServicesRequests();
                    gameInstance.sessionRequestId = "GAME";
                    gameInstance.ConfigureForSessionHeader();
                }
                return gameInstance;
            }
        }

        public static ServicesRequests EditorInstance
        {
            get
            {
                if (editorInstance == null)
                {
                    editorInstance = new ServicesRequests();
                    editorInstance.sessionRequestId = "EDITOR";
                    editorInstance.ConfigureForSessionHeader();
                }

                return editorInstance;
            }
        }

        private enum FetchModelType
        {
            Json,
            Fbx
        }

        //Sice we may want to cancel some requests, lets hold identifiers that will allow us to break out of yield functions.
        List<uint> _requestsToCancelIds;
        List<uint> RequestsToCancelIds
        {
            get
            {
                if (_requestsToCancelIds == null)
                {
                    _requestsToCancelIds = new List<uint>();
                }

                return _requestsToCancelIds;
            }
        }

        string authBearer = "";

        public void SetCurrentJWToken(string value)
        {
            authBearer = value;
        }
        public string GetCurrentJWToken()
        {
            return authBearer;
        }
        public bool IsCurrentJWTokenValid()
        {
            return true;
        }

        public delegate void OnJsonSuccessDelegate(ThreeJsModelDataObject modelJson, Texture2D skin, Texture2D normalMap);

        public delegate void OnFbxSuccessDelegate(byte[] modelFbx, Texture2D skin, Texture2D normalMap);

        public void HandleError(WWWResponseRequest wwwRequest, System.Action<System.Exception> onFailureDelegate)
        {
            HandleError(null, wwwRequest, onFailureDelegate);
        }
        public void HandleError(BaseResponseDataObject baseResponse, WWWResponseRequest wwwRequest, System.Action<System.Exception> onFailureDelegate)
        {
            if (baseResponse == null)
                baseResponse = wwwRequest.GetResponseJson<BaseResponseDataObject>();
            if (baseResponse != null && !baseResponse.IsSuccess)
            {
                onFailureDelegate(new System.Exception(wwwRequest.Error + "\n\n" + baseResponse.msg));
            }
            else onFailureDelegate(new System.Exception(wwwRequest.Error));
        }

        /// <summary>
        /// Logout, deleting session cookies (if any).
        /// </summary>
        public virtual void Logout()
        {
            Didimo.Networking.ServicesRequestsConfiguration.CurrentUserProfileDataObject = null;
            if (requestHeader.GetType().Equals(typeof(SessionRequestHeader)))
            {
                SessionRequestHeader sessionRequestHeader = (SessionRequestHeader)requestHeader;
                sessionRequestHeader.DeleteCookie();
            }
        }

        /// <summary>
        /// Login the given user with the given password.
        /// </summary>
        /// <param name="coroutineManager"></param>
        /// <param name="loginDataObject">Data object for the login.</param>
        /// <param name="onSuccessDelegate">Delegate to be called when the requests terminates with success.</param>
        /// <param name="onFailureDelegate">Delegate to be called when the requests fails.</param>
        /// <returns>An object you can yield until completion of the request.</returns>
        public object Login(CoroutineManager coroutineManager, LoginDataObject loginDataObject, System.Action onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(LoginAsync(coroutineManager, loginDataObject, onSuccessDelegate, onFailureDelegate));
        }

        private IEnumerator LoginAsync(CoroutineManager coroutineManager, LoginDataObject loginDataObject, System.Action onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            string url_path = ServicesRequestsConfiguration.DefaultConfig.loginUrl;
            //endpoint /login requires authentication bearer in JSON format
            string accessTokenFromCognitoExternalAuth = ServicesRequests.GameInstance.GetCurrentJWToken();
            string jsonToSend = "{ \"bearer\": \"" + accessTokenFromCognitoExternalAuth + "\"}";
            byte[] bytePostData = System.Text.Encoding.UTF8.GetBytes(jsonToSend);
            WWWResponseRequest wwwRequest = new WWWResponseRequest(coroutineManager, requestHeader, url_path, bytePostData, false, null, false, "application/json", false, (UploadHandler)new UploadHandlerRaw(bytePostData), (DownloadHandler)new DownloadHandlerBuffer());
            yield return wwwRequest;

            BaseResponseDataObject baseResponse = wwwRequest.GetResponseJson<BaseResponseDataObject>();

            if (wwwRequest.Error != null)
            {
                HandleError(baseResponse, wwwRequest, onFailureDelegate);
            }
            else
            {
                if (baseResponse != null && baseResponse.IsSuccess)
                {
                    onSuccessDelegate();
                }
                else if (baseResponse != null && !baseResponse.IsSuccess)
                {
                    onFailureDelegate(new System.Exception(baseResponse.msg));
                }
                else
                {
                    onFailureDelegate(new System.Exception("Server response could not be processed. You may be behind a proxy or captive portal.\nPlease check if you can visit www.mydidimo.com on your browser"));
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="coroutineManager"></param>
        /// <param name="didimoCode"></param>
        /// <param name="doneDelegate"></param>
        /// <param name="onFailureDelegate"></param>
        /// <returns></returns>
        public object GetDidimoPaginatedList(CoroutineManager coroutineManager, System.Action<PaginatedDidimoListDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(GetDidimoPaginatedListAsync(coroutineManager, ServicesRequestsConfiguration.DefaultConfig.paginatedListUrl, doneDelegate, onFailureDelegate));
        }

        IEnumerator GetDidimoPaginatedListAsync(CoroutineManager coroutineManager, string url, System.Action<PaginatedDidimoListDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest paginatedListRequest = new WWWResponseRequest(
                coroutineManager,
                requestHeader,
                url,
                false);

            yield return paginatedListRequest;
            if (paginatedListRequest.Error != null)
            {
                onFailureDelegate(new System.Exception(paginatedListRequest.Error));
            }
            else
            {
                PaginatedDidimoListDataObject paginatedDidimoListDataObject = paginatedListRequest.GetResponseJson<PaginatedDidimoListDataObject>();
                doneDelegate(paginatedDidimoListDataObject);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coroutineManager"></param>
        /// <param name="didimoCode"></param>
        /// <param name="doneDelegate"></param>
        /// <param name="onFailureDelegate"></param>
        /// <returns></returns>
        public object DidimoPaginatedListForCursor(CoroutineManager coroutineManager, string cursor, System.Action<PaginatedDidimoListDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(DidimoPaginatedListForCursorAsync(coroutineManager, cursor, doneDelegate, onFailureDelegate));
        }

        IEnumerator DidimoPaginatedListForCursorAsync(CoroutineManager coroutineManager, string cursor, System.Action<PaginatedDidimoListDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            cursor = cursor.Replace("\\", "");
            string url;
            if (cursor.StartsWith("/") && ServicesRequestsConfiguration.DefaultConfig.apiUrl.EndsWith("/"))
            {
                url = ServicesRequestsConfiguration.DefaultConfig.apiUrl + cursor.Remove(0, 1);
            }
            else url = ServicesRequestsConfiguration.DefaultConfig.apiUrl + cursor;

            WWWResponseRequest paginatedListRequest = new WWWResponseRequest(
                coroutineManager,
                requestHeader,
                url,
                false);
            yield return paginatedListRequest;
            if (paginatedListRequest.Error != null)
            {
                onFailureDelegate(new System.Exception(paginatedListRequest.Error));
            }
            else
            {
                PaginatedDidimoListDataObject paginatedDidimoListDataObject = paginatedListRequest.GetResponseJson<PaginatedDidimoListDataObject>();
                doneDelegate(paginatedDidimoListDataObject);
            }
        }

        /// <summary>
        /// Delete the specified Didimo.
        /// </summary>
        /// <param name="coroutineManager"></param>
        /// <param name="didimoCode">The code of the Didimo we want to delete.</param>
        /// <param name="onSuccessDelegate">Delegate to be called when the requests terminates with success.</param>
        /// <param name="onFailureDelegate">Delegate to be called when the requests fails.</param>
        /// <returns>An object you can yield until completion of the request.</returns>
        public object DeleteDidimo(CoroutineManager coroutineManager, string didimoCode, System.Action onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(DeleteDidimoAsync(coroutineManager, didimoCode, onSuccessDelegate, onFailureDelegate));
        }

        private IEnumerator DeleteDidimoAsync(CoroutineManager coroutineManager, string didimoCode, System.Action onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest wwwRequest = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.DeleteDidimoForDidimoCode(didimoCode), false);

            yield return wwwRequest;

            if (wwwRequest.Error != null)
            {
                HandleError(wwwRequest, onFailureDelegate);
            }
            else
            {
                onSuccessDelegate();
            }
        }

        /// <summary>
        /// Get the profile of the current user.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="onSuccessDelegate">Delegate to be called when the requests terminates with success. Returns a <see cref=""/></param>
        /// <param name="onFailureDelegate">Delegate to be called when the requests fails.</param>
        /// <returns>An object you can yield until completion of the request.</returns>
        public object Profile(CoroutineManager coroutineManager, System.Action<UserProfileDataObject> onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(ProfileAsync(coroutineManager, onSuccessDelegate, onFailureDelegate));
        }

        protected virtual IEnumerator ProfileAsync(CoroutineManager coroutineManager, System.Action<UserProfileDataObject> onSuccessDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest wwwRequest = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.profileUrl, false);

            yield return wwwRequest;

            if (wwwRequest.Error != null)
            {
                HandleError(wwwRequest, onFailureDelegate);
            }
            else
            {
                UserProfileDataObject updo = wwwRequest.GetResponseJson<UserProfileDataObject>();
                Didimo.Networking.ServicesRequestsConfiguration.CurrentUserProfileDataObject = updo;
                onSuccessDelegate(wwwRequest.GetResponseJson<UserProfileDataObject>());
            }
        }

        /// <summary>
        /// Fetch the files for the model in fbx format, with all the required textures.
        /// </summary>
        /// <param name="didimoCode">The didimo code.</param>
        /// <param name="onFbxSuccessDelegate">Delegate to be called when the request terminates with success.</param>
        /// <param name="onFailureDelegate">Delegate to be called when the request fails.</param>
        public object FetchDidimoZip(CoroutineManager coroutineManager, string typeOfZip, string didimoCode, System.Action<string, byte[]> onSuccessDelegate, System.Action<System.Exception> onFailureDelegate, DownloadHandler downloadHandler = null)
        {
            return coroutineManager.StartCoroutine(FetchDidimoZipAsync(coroutineManager, typeOfZip, didimoCode, onSuccessDelegate, onFailureDelegate, downloadHandler));
        }

        private IEnumerator FetchDidimoZipAsync(CoroutineManager coroutineManager, string typeOfZip, string didimoCode, System.Action<string, byte[]> onSuccessDelegate, System.Action<System.Exception> onFailureDelegate, DownloadHandler downloadHandler = null)
        {
            string url = ServicesRequestsConfiguration.DefaultConfig.DidimoZipUrlForDidimoCode(typeOfZip, didimoCode);
            WWWResponseRequest wwwRequest;

            wwwRequest = new WWWResponseRequest(coroutineManager, requestHeader, url, true, null, true, false);

            yield return wwwRequest;

            if (wwwRequest.Error != null)
            {
                Debug.LogError("Error downloading Didimo zip file. " + wwwRequest.Error);
                HandleError(wwwRequest, onFailureDelegate);
            }
            else
            {
                //onSuccessDelegate(wwwRequest.GetResponseBytes());

                WWWResponseRequest wwwRequestS3;
                string zippath = "";

                //parse jsonResponse and get location url
                S3DownloadLinkDataObject s3DownloadLinkDataObject = wwwRequest.GetResponseJson<S3DownloadLinkDataObject>();
                //correct url by removing duplicated forward slashes 
                string s3DownloadLinkDataObject_correctedUrl = s3DownloadLinkDataObject.location.Replace("\\", "");
                zippath = WWWCachedRequest.GetCachePath(s3DownloadLinkDataObject_correctedUrl, null);
                Debug.Log("Corrected S3 location: " + s3DownloadLinkDataObject_correctedUrl);
                //make a request to download from the provided location
                wwwRequestS3 = new WWWResponseRequest(
                    coroutineManager,
                    requestHeader,
                    s3DownloadLinkDataObject_correctedUrl,
                    true, //cached
                    null, //donedelegate
                    false, //true, //hasAuthentication  //BUG FIX: why was this set to true? does s3 require authentication?
                    true, //autosavecache
                    downloadHandler);

                //wait for response
                yield return wwwRequestS3;

                if (wwwRequestS3.Error != null)
                {
                    Debug.LogError("Error downloading Didimo zip file from S3. " + wwwRequestS3.Error);
                    wwwRequest.DeleteCacheOfRequest();
                    HandleError(wwwRequestS3, onFailureDelegate);
                }
                else
                {
                    //load response and continue with the action delegates
                    wwwRequest.SaveRequestToCache();
                    onSuccessDelegate(zippath, wwwRequestS3.GetResponseBytes());
                }

            }
        }

        /// <summary>
        /// Upload the photo and check for didimo creation progress. It will be automatically scaled to the max supported resolution.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="photo">The photo used to create the Didimo.</param>
        /// <param name="onDidimoCreatedSuccess">Delegate that is called when the operation terminates successfully. Returns the Didimo code.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        /// <param name="onProgressUpdateDelegate">Delegate that is called to notify the current progress of the didimo generation process. Not triggered for photo upload progress.</param>
        public void CreateDidimoCheckProgress(CoroutineManager coroutineManager, Texture2D photo, string scanName, System.Action<string> onDidimoCreatedSuccess, System.Action<System.Exception> onFailureDelegate, System.Action<float> onProgressUpdateDelegate)
        {
            if (photo.width * photo.height > MAX_PHOTO_PIXELS)
            {
                if (!TextureScaler.Scale(photo, MAX_PHOTO_PIXELS))
                {
                    onFailureDelegate(new System.Exception(scaling_error_message));
                    return;
                }
            }
            //Do not return the coroutine, as this coroutine will spawn inner coroutines that wouldn't be canceled if the main one was canceled, resulting in weird behaviour.
            coroutineManager.StartCoroutine(CreateDidimoCheckProgressAsync(coroutineManager, InputType.Photo, photo.EncodeToJPG(91), scanName, onDidimoCreatedSuccess, onFailureDelegate, onProgressUpdateDelegate));
        }

        /// <summary>
        /// Upload the photo or scan zip and and check for didimo creation progress.
        /// The photo must be jpg.
        /// The zip must contain mesh.jpg and mesh.obj files. The names are case sensitive.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="file">The photo or scan zip used to create the Didimo.</param>
        /// <param name="onDidimoCreatedSuccess">Delegate that is called when the operation terminates successfully. Returns the Didimo code.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        /// <param name="onProgressUpdateDelegate">Delegate that is called to notify the current progress of the didimo generation process. Not triggered for photo upload progress.</param>
        public void CreateDidimoCheckProgress(CoroutineManager coroutineManager, InputType inputType, byte[] file, string scanName, System.Action<string> onDidimoCreatedSuccess, System.Action<System.Exception> onFailureDelegate, System.Action<float> onProgressUpdateDelegate)
        {
            //Do not return the coroutine, as this coroutine will spawn inner coroutines that wouldn't be canceled if the main one was canceled, resulting in weird behaviour.
            coroutineManager.StartCoroutine(CreateDidimoCheckProgressAsync(coroutineManager, inputType, file, scanName, onDidimoCreatedSuccess, onFailureDelegate, onProgressUpdateDelegate));
        }

        /// <summary>
        /// Upload the photo and and check for didimo creation progress.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="photo">Path to the photo used to create the Didimo.</param>
        /// <param name="onDidimoCreatedSuccess">Delegate that is called when the operation terminates successfully. Returns the Didimo code.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        /// <param name="onProgressUpdateDelegate">Delegate that is called to notify the current progress of the didimo generation process. Not triggered for photo upload progress.</param>
        public void CreateDidimoCheckProgress(CoroutineManager coroutineManager, InputType inputType, string photoPath, System.Action<string> onDidimoCreatedSuccess, System.Action<System.Exception> onFailureDelegate, System.Action<float> onProgressUpdateDelegate)
        {
            coroutineManager.StartCoroutine(CreateDidimoCheckProgressAsync(coroutineManager, inputType, photoPath, onDidimoCreatedSuccess, onFailureDelegate, onProgressUpdateDelegate));
        }

        private IEnumerator CreateDidimoCheckProgressAsync(CoroutineManager coroutineManager, InputType inputType, string photoPath, System.Action<string> onDidimoCreatedSuccess, System.Action<System.Exception> onFailureDelegate, System.Action<float> onProgressUpdateDelegate)
        {
            WWWResponseRequest photoWWW = new WWWResponseRequest(coroutineManager, requestHeader, "file:///" + photoPath, false);
            yield return photoWWW;

            if (photoWWW.Error != null)
            {
                Debug.LogError(photoWWW.Error);
                HandleError(photoWWW, onFailureDelegate);
                yield break;
            }

            Texture2D photo = photoWWW.GetResponseTexture();
            if (photo.width * photo.height > MAX_PHOTO_PIXELS)
            {
                if (!TextureScaler.Scale(photo, MAX_PHOTO_PIXELS))
                {
                    onFailureDelegate(new System.Exception(scaling_error_message));
                    yield break;
                }
            }

            // Unity strips Exif data. If we find Exif data in the original image, copy it over to the texture generated by unity
            byte[] jpgRaw = photoWWW.GetResponseBytes();
            byte[] exifData = ExifUtils.GetExifData(photoPath);

            if (exifData != null)
            {
                jpgRaw = ExifUtils.AddExifData(jpgRaw, exifData);
            }

            yield return coroutineManager.StartCoroutine(CreateDidimoCheckProgressAsync(coroutineManager, inputType, photo.EncodeToJPG(91), null, onDidimoCreatedSuccess, onFailureDelegate, onProgressUpdateDelegate));
            Object.Destroy(photo);
        }

        private IEnumerator CreateDidimoCheckProgressAsync(CoroutineManager coroutineManager, InputType inputType, byte[] file, string scanName,
            System.Action<string> onDidimoCreatedSuccess,
            System.Action<System.Exception> onFailureDelegate,
            System.Action<float> onProgressUpdateDelegate)
        {
            string didimoCode = null;
            bool abort = false;

            object createDidimoRequest = CreateDidimo(
                coroutineManager,
                inputType,
                file,
                dCode =>
                {
                    didimoCode = dCode;
                    if (scanName != null)
                        SetDidimoLabel(
                           coroutineManager,
                           didimoCode,
                           scanName,
                           () =>
                           {
                               Debug.Log("Set didimo label OK: " + didimoCode + " - " + scanName);
                           },
                           exception =>
                           {
                               Debug.LogError("Set didimo label exception: " + didimoCode + " - " + scanName + " -> " + exception.Message);
                               ErrorOverlay.Instance.ShowError("Error setting didimo name.\r\n" + exception.Message);
                           });
                },
                didimoProgressError =>
                {
                    onFailureDelegate(didimoProgressError);
                    abort = true;
                }
                );

            yield return createDidimoRequest;

            if (abort)
            {
                yield break;
            }



            object creationProgressRequest = DidimoCreationProgress(
                    coroutineManager,
                    didimoCode,
                   () =>
                   {
                       onDidimoCreatedSuccess(didimoCode);
                   },
                  progress =>
                  {
                      onProgressUpdateDelegate(progress);
                  }, error =>
                  {
                      onFailureDelegate(error);
                      abort = true;
                  });

            yield return creationProgressRequest;
        }

        /// <summary>
        /// Create a Didimo by sending a photo, and obtain the didimoKey. With the key you can check the progress of the creation of the didimo.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="photoPath">The path of the photo used to create the Didimo.</param>
        /// <param name="onDidimoCreatedWithCodeDelegate">Delegate that is called when the operation terminates successfully.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        public object CreateDidimo(CoroutineManager coroutineManager, string photoPath, System.Action<string> onDidimoCreatedWithCodeDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(CreateDidimoAsync(coroutineManager, photoPath, onDidimoCreatedWithCodeDelegate, onFailureDelegate));
        }

        IEnumerator CreateDidimoAsync(CoroutineManager coroutineManager, string photoPath, System.Action<string> onDidimoCreatedWithCodeDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest photoWWW = new WWWResponseRequest(coroutineManager, requestHeader, "file:///" + photoPath, false);
            yield return photoWWW;

            if (photoWWW.Error != null)
            {
                Debug.LogError(photoWWW.Error);
                HandleError(photoWWW, onFailureDelegate);

                yield break;
            }

            Texture2D photo = photoWWW.GetResponseTexture();
            if (photo.width * photo.height > MAX_PHOTO_PIXELS)
            {
                if (!TextureScaler.Scale(photo, MAX_PHOTO_PIXELS))
                {
                    onFailureDelegate(new System.Exception(scaling_error_message));
                    yield break;
                }
            }

            // Unity strips Exif data. If we find Exif data in the original image, copy it over to the texture generated by unity
            byte[] jpgRaw = photo.EncodeToJPG(91);
            byte[] exifData = ExifUtils.GetExifData(photoPath);

            if (exifData != null)
            {
                jpgRaw = ExifUtils.AddExifData(jpgRaw, exifData);
            }

            yield return coroutineManager.StartCoroutine(CreateDidimoAsync(coroutineManager, InputType.Photo, jpgRaw, onDidimoCreatedWithCodeDelegate, onFailureDelegate));
        }

        /// <summary>
        /// Create a Didimo by sending a photo, and obtain the didimoKey. With the key you can check the progress of the creation of the didimo. Will scale the input photo to the max supported resolution.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="photo">The photo used to create the Didimo. It will be auto scaled to the max supported resolution. This will change the texture itself, so be careful.</param>
        /// <param name="onDidimoCreatedWithCodeDelegate">Delegate that is called when the operation terminates successfully.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        public object CreateDidimo(CoroutineManager coroutineManager, InputType inputType, Texture2D photo, System.Action<string> onDidimoCreatedWithCodeDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            if (photo.width * photo.height > MAX_PHOTO_PIXELS)
            {
                if (!TextureScaler.Scale(photo, MAX_PHOTO_PIXELS))
                {
                    onFailureDelegate(new System.Exception(scaling_error_message));
                    return null;
                }
            }
            return coroutineManager.StartCoroutine(CreateDidimoAsync(coroutineManager, inputType, photo.EncodeToJPG(91), onDidimoCreatedWithCodeDelegate, onFailureDelegate));
        }

        /// <summary>
        /// Create a Didimo by sending a photo, and obtain the didimoKey. With the key you can check the progress of the creation of the didimo.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="photo">The photo used to create the Didimo. It will NOT be auto scaled to the max supported resolution, so you must make sure this is done beforehand.</param>
        /// <param name="onDidimoCreatedWithCodeDelegate">Delegate that is called when the operation terminates successfully.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        public object CreateDidimo(CoroutineManager coroutineManager, InputType inputType, byte[] photo, System.Action<string> onDidimoCreatedWithCodeDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(CreateDidimoAsync(coroutineManager, inputType, photo, onDidimoCreatedWithCodeDelegate, onFailureDelegate));
        }

        IEnumerator CreateDidimoAsync(CoroutineManager coroutineManager, InputType inputType, byte[] photo, System.Action<string> onDidimoCreatedWithCodeDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", photo);

            string url = "";

            switch (inputType)
            {
                case InputType.Photo: url = ServicesRequestsConfiguration.DefaultConfig.GetUploadPhotoUrl(); break;
                case InputType.LoFiMesh: url = ServicesRequestsConfiguration.DefaultConfig.GetUploadLoFiMeshUrl(); break;
                case InputType.HiFiMesh: url = ServicesRequestsConfiguration.DefaultConfig.GetUploadHiFiMeshUrl(); break;
                default: break;
            }

            WWWResponseRequest uploadWWW = new WWWResponseRequest(coroutineManager, requestHeader, url, form);
            yield return uploadWWW;

            if (uploadWWW.Error != null)
            {
                Debug.LogError(uploadWWW.Error);
                HandleError(uploadWWW, onFailureDelegate);
                yield break;
            }
            else
            {
                CreateDidimoDataObject createDidimoDO = uploadWWW.GetResponseJson<CreateDidimoDataObject>();
                Debug.Log("Creating Didimo with code: " + createDidimoDO.didimoCode);
                onDidimoCreatedWithCodeDelegate(createDidimoDO.didimoCode);
            }
        }

        /// <summary>
        /// Check the creation progress of a didimo.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="didimoCode">The code of the Didimo we want to check the progress of.</param>
        /// <param name="doneDelegate">Delegate that is called when the Didimo is created.</param>
        /// <param name="onProgressUpdateDelegate">Delegate that is called to notify the current progress of the didimo generation process. Not triggered for photo upload progress.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        /// <returns>A Unique identifier of this request. Use it to cancel the request.</returns>
        public object DidimoCreationProgress(CoroutineManager coroutineManager, string didimoCode, System.Action doneDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(CheckProgressAsync(coroutineManager, didimoCode, doneDelegate, onProgressUpdateDelegate, onFailureDelegate));
        }
        IEnumerator CheckProgressAsync(CoroutineManager coroutineManager, string didimoCode, System.Action doneDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            yield return CheckProgressAsync(coroutineManager, didimoCode, true, true, doneDelegate, onProgressUpdateDelegate, onFailureDelegate);
        }
        IEnumerator CheckProgressAsync(CoroutineManager coroutineManager, string didimoCode, bool shouldDelayForPreviewGeneration, bool isAvatar, System.Action doneDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            while (true)
            {
                Debug.Log("CheckProgressAsync");
                WWWResponseRequest checkProgressWWW = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.StatusUrlForDidimoCode(didimoCode), false);
                yield return checkProgressWWW;

                if (checkProgressWWW.Error != null)
                {
                    HandleError(checkProgressWWW, onFailureDelegate);
                    yield break;
                }
                else
                {
                    DidimoStatusDataObject didimoStatus = checkProgressWWW.GetResponseJson<DidimoStatusDataObject>();
                    if (didimoStatus.percent == 100)
                    {
                        onProgressUpdateDelegate(didimoStatus.percent);
                        doneDelegate();
                        yield break;
                    }
                    else
                    {
                        onProgressUpdateDelegate(didimoStatus.percent);
                    }
                }

                if (shouldDelayForPreviewGeneration)
                    yield return coroutineManager.WaitForSecondsRealtime(isAvatar ? checkAvatarGenerationProgressIntervalInSeconds : checkAssetGenerationProgressIntervalInSeconds);
            }
        }

        public object GetDidimoDefinitionModel(CoroutineManager coroutineManager, string didimoCode, System.Action<DidimoStatusDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(GetDidimoDefinitionModelAsync(coroutineManager, didimoCode, doneDelegate, onFailureDelegate));
        }
        IEnumerator GetDidimoDefinitionModelAsync(CoroutineManager coroutineManager, string didimoCode, System.Action<DidimoStatusDataObject> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest checkProgressWWW = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.StatusUrlForDidimoCode(didimoCode), false);
            yield return checkProgressWWW;

            if (checkProgressWWW.Error != null)
            {
                HandleError(checkProgressWWW, onFailureDelegate);
                yield break;
            }
            else
            {
                DidimoStatusDataObject didimoStatus = checkProgressWWW.GetResponseJson<DidimoStatusDataObject>();
                if (didimoStatus.percent == 100)
                {
                    doneDelegate(didimoStatus);
                    yield break;
                }
                else
                {
                    onFailureDelegate(new System.Exception("The requested didimo has not finished processing!"));
                    yield break;
                }
            }
        }


        /// <summary>
        /// Download one of the didimo previews (a render of the final output)
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="previewType">The type of the preview.</param>
        /// <param name="didimoCode">The code of the didimo we want to preview.</param>
        /// <param name="doneDelegate">Delegate to be called once the preview has been downloaded. Returns the preview texture.</param>
        /// <param name="onFailureDelegate">Preview to be calledwhen the operation terminates with an error.</param>
        /// <returns></returns>
        public object DownloadDidimoPreview(CoroutineManager coroutineManager, PreviewType previewType, string didimoCode, System.Action<Texture2D> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            string url = null;
            switch (previewType)
            {
                case PreviewType.FrontPerspective:

                    url = ServicesRequestsConfiguration.DefaultConfig.FrontPreviewUrlForDidimoCode(didimoCode);
                    break;
                default: break;
            }

            return coroutineManager.StartCoroutine(DownloadImageAsync(coroutineManager, url, doneDelegate, onFailureDelegate));
        }

        /// <summary>
        /// Download the thumbnail image of the given Didimo.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="didimoCode">The code of the Didimo we want to download the preview of.</param>
        /// <param name="doneDelegate">Delegate to be called when the operation is completed. Returns a Texture2D.</param>
        /// <param name="onFailureDelegate">Preview to be calledwhen the operation terminates with an error. Returns an axception.</param>
        /// <returns></returns>
        public object DownloadDidimoThumbnail(CoroutineManager coroutineManager, string didimoCode, System.Action<Texture2D> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            string url = ServicesRequestsConfiguration.DefaultConfig.PhotoUrlForDidimoCode(didimoCode);
            return coroutineManager.StartCoroutine(DownloadImageAsync(coroutineManager, url, doneDelegate, onFailureDelegate));

        }

        IEnumerator DownloadImageAsync(CoroutineManager coroutineManager, string url, System.Action<Texture2D> doneDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            WWWResponseRequest didimoPreviewRequest = new WWWResponseRequest(
                coroutineManager,
                requestHeader,
                url,
                true, null, true);

            yield return didimoPreviewRequest;
            if (didimoPreviewRequest.Error != null)
            {
                HandleError(didimoPreviewRequest, onFailureDelegate);
            }
            else
            {
                WWWResponseRequest didimoPreviewRequest2;

                //parse jsonResponse and get location url
                S3DownloadLinkDataObject s3DownloadLinkDataObject = didimoPreviewRequest.GetResponseJson<S3DownloadLinkDataObject>();
                //correct url by removing duplicated forward slashes 
                string s3DownloadLinkDataObject_correctedUrl = s3DownloadLinkDataObject.location.Replace("\\", "");
                //Debug.Log("Corrected S3 location: " + s3DownloadLinkDataObject_correctedUrl);
                //make a request to download from the provided location
                didimoPreviewRequest2 = new WWWResponseRequest(
                coroutineManager,
                requestHeader,
                s3DownloadLinkDataObject_correctedUrl,
                true,
                null,
                false);

                //wait for response
                yield return didimoPreviewRequest2;

                if (didimoPreviewRequest2.Error != null)
                {
                    Debug.LogError("Error downloading preview image from S3 server.\n "+ s3DownloadLinkDataObject_correctedUrl+"\n" + didimoPreviewRequest2.Error);
                    didimoPreviewRequest.DeleteCacheOfRequest();
                    HandleError(didimoPreviewRequest2, onFailureDelegate);
                }
                else
                {
                    //load response and continue with the action delegates
                    //load response as texture
                    Texture2D texture = didimoPreviewRequest2.GetResponseTexture();
                    //continue with the action delegates
                    if (texture != null)
                    {
                        didimoPreviewRequest.SaveRequestToCache();
                        doneDelegate(texture);
                    }
                    else
                    {
                        //if the download fails then the first request must be deleted or else it will point to an expired s3 link which is not on cache
                        didimoPreviewRequest.DeleteCacheOfRequest();
                        //continue and process exception normally
                        onFailureDelegate(new System.Exception());
                    }
                }
            }
        }

        /// <summary>
        /// Updates the scan label for the didimo with the given code.
        /// </summary>
        /// <param name="didimoCode">The code of the didimo.</param>
        /// <param name="didimoLabel">The code of the didimo.</param>
        /// <param name="action">Action to perform when done.</param>
        /// <param name="errorAction">Action to perform when an error occurs.</param>
        public object SetDidimoLabel(CoroutineManager coroutineManager, string didimoCode, string didimoLabel, System.Action action, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(SetDidimoLabelAsync(coroutineManager, didimoCode, didimoLabel, action, onFailureDelegate));
        }

        IEnumerator SetDidimoLabelAsync(CoroutineManager coroutineManager, string didimoCode, string didimoLabel, System.Action action, System.Action<System.Exception> onFailureDelegate)
        {
            WWWForm form = new WWWForm();
            form.AddField("name", didimoLabel);

            WWWResponseRequest wwwRequest = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.SetPropertiesUrlForDidimoCode(didimoCode), form);
            yield return wwwRequest;

            if (wwwRequest.Error != null)
            {
                HandleError(wwwRequest, onFailureDelegate);
            }
            else
            {
                BaseResponseDataObject baseResponse = wwwRequest.GetResponseJson<BaseResponseDataObject>();

                if (baseResponse != null && baseResponse.IsSuccess)
                {
                    action();
                }
                else if (baseResponse != null && !baseResponse.IsSuccess)
                {
                    onFailureDelegate(new System.Exception(baseResponse.msg));
                }
                else
                {
                    onFailureDelegate(new System.Exception("Server response could not be processed. You may be behind a proxy or captive portal.\nPlease check if you can visit www.mydidimo.com on your browser"));
                }
            }
        }


        /// <summary>
        /// Send vertex and receive deformed vertex positions.
        /// The vertex list must be sent as byte array.
        /// </summary>
        /// <param name="coroutineManager">A coroutine manager. Responsible to start coroutines.</param>
        /// <param name="didimoCode">The code of the didimo.</param>
        /// <param name="vertex">The vertex of the mesh to deform.</param>
        /// <param name="onDidimoCreatedSuccess">Delegate that is called when the operation terminates successfully. Returns the deformed vertex.</param>
        /// <param name="onFailureDelegate">Delegate that is called when the operation terminates with an error.</param>
        public object DeformDidimoAsset(CoroutineManager coroutineManager, string didimoCode, string hairId, byte[] vertex, System.Action<byte[]> onSuccessDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            return coroutineManager.StartCoroutine(DeformDidimoAssetAsync(coroutineManager, didimoCode, hairId, vertex, onSuccessDelegate, onProgressUpdateDelegate, onFailureDelegate));
        }

        private IEnumerator DeformDidimoAssetAsync(CoroutineManager coroutineManager, string didimoCode, string hairId, byte[] vertex, System.Action<byte[]> onSuccessDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            if (vertex == null || vertex.Length == 0)
            {
                onFailureDelegate(new System.Exception("Invalid input - null or empty data"));
                yield break;
            }
            int requestHash = System.Convert.ToBase64String(vertex).GetHashCode();
            string url = ServicesRequestsConfiguration.DefaultConfig.DeformUrlForDidimoCode(didimoCode, hairId);
            WWWResponseRequest uploadWWW = new WWWResponseRequest(coroutineManager, requestHeader, url, vertex, true, null, true, "application/didimo-vertex", false);
            yield return uploadWWW;

            if (uploadWWW.Error != null)
            {
                Debug.LogError(uploadWWW.Error);
                HandleError(uploadWWW, onFailureDelegate);
                yield break;
            }
            else
            {
                //the API returns an asset key produced by the pipeline
                BaseResponseDataObject baseResponse = uploadWWW.GetResponseJson<BaseResponseDataObject>();
                if (baseResponse != null && baseResponse.IsSuccess)
                {
                    //check asset creation progress (status) and download when ready
                    VertexDeformDataObject vertexDeform = uploadWWW.GetResponseJson<VertexDeformDataObject>();
                    Debug.Log("Creating deformed asset with code: " + vertexDeform.deformedAssetKey);
                    string deformedAssetKey = vertexDeform.deformedAssetKey;

                    string downloadURL = ServicesRequestsConfiguration.DefaultConfig.DeformedAssetUrlForKey(deformedAssetKey);

                    bool isCached = WWWCachedRequest.IsCachedOrLocalFile(downloadURL, null);
                    bool hasFailed = false;
                    bool hasCompleted = false;

                    if (!isCached)
                    {
                        object deformationProgressRequest = CheckProgressAsync(coroutineManager,
                                                                                deformedAssetKey,
                                                                                true,
                                                                                false,
                                                                                () =>
                                                                                {
                                                                                    //Debug.Log("deformed asset status completed");
                                                                                    hasCompleted = true;
                                                                                },
                                                                                progress =>
                                                                                {
                                                                                    onProgressUpdateDelegate(progress);
                                                                                }, error =>
                                                                                {
                                                                                    //Debug.Log("deformed asset status error");
                                                                                    hasFailed = true;
                                                                                    //if the download fails then the first request must be deleted or else it will point to an expired s3 link which is not on cache
                                                                                    uploadWWW.DeleteCacheOfRequest();
                                                                                    onFailureDelegate(error);
                                                                                });
                        yield return deformationProgressRequest;
                    }
                    else Debug.Log("Deformation - isCached!! Proceeding!");

                    if (isCached || (!hasFailed && hasCompleted))
                    {
                        //proceed with download
                        WWWResponseRequest deformedAssetWWW = new WWWResponseRequest(coroutineManager, requestHeader, downloadURL, true, null, true, false);
                        yield return deformedAssetWWW;

                        if (deformedAssetWWW.Error != null)
                        {
                            Debug.LogError(deformedAssetWWW.Error);
                            HandleError(deformedAssetWWW, onFailureDelegate);
                            yield break;
                        }
                        else
                        {
                            //parse jsonResponse and get location url
                            S3DownloadLinkDataObject s3DownloadLinkDataObject = deformedAssetWWW.GetResponseJson<S3DownloadLinkDataObject>();
                            //correct url by removing duplicated forward slashes 
                            string s3DownloadLinkDataObject_correctedUrl = s3DownloadLinkDataObject.location.Replace("\\", "");
                            //cachedpath = WWWCachedRequest.GetCachePath(s3DownloadLinkDataObject_correctedUrl, null, -1); ;
                            //Debug.Log("Corrected S3 location: " + s3DownloadLinkDataObject_correctedUrl);
                            //make a request to download from the provided location
                            WWWResponseRequest wwwRequestS3 = new WWWResponseRequest(coroutineManager,
                                                                                     requestHeader,
                                                                                     s3DownloadLinkDataObject_correctedUrl,
                                                                                     true,
                                                                                     null,
                                                                                     false);

                            //wait for response
                            yield return wwwRequestS3;

                            if (wwwRequestS3.Error != null)
                            {
                                //Debug.LogError("Error downloading deformed vertex from S3. " + wwwRequestS3.Error);
                                deformedAssetWWW.DeleteCacheOfRequest();
                                HandleError(wwwRequestS3, onFailureDelegate);
                            }
                            else
                            {
                                Debug.Log("Success - downloaded deformed vertex from S3");
                                uploadWWW.SaveRequestToCache();
                                deformedAssetWWW.SaveRequestToCache();
                                //load response and continue with the action delegates
                                onSuccessDelegate(wwwRequestS3.GetResponseBytes());
                            }
                        }
                    }
                }
                else if (baseResponse != null && !baseResponse.IsSuccess)
                {
                    //if the download fails then the first request must be deleted or else it will point to an expired s3 link which is not on cache
                    uploadWWW.DeleteCacheOfRequest();
                    onFailureDelegate(new System.Exception(baseResponse.msg));
                }
                else
                {
                    onFailureDelegate(new System.Exception("Server response could not be processed. You may be behind a proxy or captive portal.\nPlease check if you can visit www.mydidimo.com on your browser"));
                }
            }
        }

        public IEnumerator DeformDidimoAssetAsync_Editor(CoroutineManager coroutineManager, string didimoCode, string hairId, byte[] vertex, System.Action<byte[]> onSuccessDelegate, System.Action<float> onProgressUpdateDelegate, System.Action<System.Exception> onFailureDelegate)
        {
            if (vertex == null || vertex.Length == 0)
            {
                onFailureDelegate(new System.Exception("Invalid input - null or empty data"));
                yield break;
            }
            int requestHash = System.Convert.ToBase64String(vertex).GetHashCode();
            string url = ServicesRequestsConfiguration.DefaultConfig.DeformUrlForDidimoCode(didimoCode, hairId);
            WWWResponseRequest uploadWWW = new WWWResponseRequest(coroutineManager, requestHeader, url, vertex, true, null, true, "application/didimo-vertex", false);
            yield return uploadWWW;

            if (uploadWWW.Error != null)
            {
                Debug.LogError(uploadWWW.Error);
                HandleError(uploadWWW, onFailureDelegate);
                yield break;
            }
            else
            {
                //onSuccessDelegate(uploadWWW.GetResponseBytes());

                //now the API returns an asset key produced by the pipeline
                BaseResponseDataObject baseResponse = uploadWWW.GetResponseJson<BaseResponseDataObject>();
                if (baseResponse != null && baseResponse.IsSuccess)
                {
                    //check asset creation progress (status) and download when ready
                    VertexDeformDataObject vertexDeform = uploadWWW.GetResponseJson<VertexDeformDataObject>();
                    Debug.Log("Creating deformed asset with code: " + vertexDeform.deformedAssetKey);
                    string deformedAssetKey = vertexDeform.deformedAssetKey;

                    string downloadURL = ServicesRequestsConfiguration.DefaultConfig.DeformedAssetUrlForKey(deformedAssetKey);

                    bool isCached = WWWCachedRequest.IsCachedOrLocalFile(downloadURL, null);
                    if (!isCached)
                    {
                        while (true)
                        {
                            WWWResponseRequest checkProgressWWW = new WWWResponseRequest(coroutineManager, requestHeader, ServicesRequestsConfiguration.DefaultConfig.StatusUrlForDidimoCode(deformedAssetKey), false);
                            yield return checkProgressWWW;

                            if (checkProgressWWW.Error != null)
                            {
                                Debug.LogError(checkProgressWWW.Error);
                                HandleError(checkProgressWWW, onFailureDelegate);
                                yield break;
                            }
                            else
                            {
                                DidimoStatusDataObject deformedAssetStatus = checkProgressWWW.GetResponseJson<DidimoStatusDataObject>();
                                if (deformedAssetStatus.percent == 100)
                                {
                                    onProgressUpdateDelegate(deformedAssetStatus.percent);

                                    //proceed with download
                                    WWWResponseRequest deformedAssetWWW = new WWWResponseRequest(coroutineManager, requestHeader, downloadURL, true, null, true, false);
                                    yield return deformedAssetWWW;

                                    if (deformedAssetWWW.Error != null)
                                    {
                                        Debug.LogError(deformedAssetWWW.Error);
                                        HandleError(deformedAssetWWW, onFailureDelegate);
                                        yield break;
                                    }
                                    else
                                    {
                                        //parse jsonResponse and get location url
                                        S3DownloadLinkDataObject s3DownloadLinkDataObject = deformedAssetWWW.GetResponseJson<S3DownloadLinkDataObject>();
                                        //correct url by removing duplicated forward slashes 
                                        string s3DownloadLinkDataObject_correctedUrl = s3DownloadLinkDataObject.location.Replace("\\", "");
                                        //cachedpath = WWWCachedRequest.GetCachePath(s3DownloadLinkDataObject_correctedUrl, null, -1); ;
                                        //Debug.Log("Corrected S3 location: " + s3DownloadLinkDataObject_correctedUrl);
                                        //make a request to download from the provided location
                                        WWWResponseRequest wwwRequestS3 = new WWWResponseRequest(coroutineManager,
                                                                                                    requestHeader,
                                                                                                    s3DownloadLinkDataObject_correctedUrl,
                                                                                                    true,
                                                                                                    null,
                                                                                                    false);

                                        //wait for response
                                        yield return wwwRequestS3;

                                        if (wwwRequestS3.Error != null)
                                        {
                                            //Debug.LogError("Error downloading deformed vertex from S3. " + wwwRequestS3.Error);
                                            deformedAssetWWW.DeleteCacheOfRequest();
                                            HandleError(wwwRequestS3, onFailureDelegate);
                                        }
                                        else
                                        {
                                            //Debug.Log("Success - downloaded deformed vertex from S3");
                                            uploadWWW.SaveRequestToCache();
                                            deformedAssetWWW.SaveRequestToCache();
                                            //load response and continue with the action delegates
                                            onSuccessDelegate(wwwRequestS3.GetResponseBytes());
                                        }
                                    }
                                    yield break;
                                }
                                else
                                {
                                    onProgressUpdateDelegate(deformedAssetStatus.percent);
                                }
                            }
                            yield return coroutineManager.WaitForSecondsRealtime(checkAssetGenerationProgressIntervalInSeconds);
                        }
                    }

                    if (isCached)
                    {
                        //proceed with download
                        WWWResponseRequest deformedAssetWWW = new WWWResponseRequest(coroutineManager, requestHeader, downloadURL, true, null, true, false);
                        yield return deformedAssetWWW;

                        if (deformedAssetWWW.Error != null)
                        {
                            Debug.LogError(deformedAssetWWW.Error);
                            HandleError(deformedAssetWWW, onFailureDelegate);
                            yield break;
                        }
                        else
                        {
                            //parse jsonResponse and get location url
                            S3DownloadLinkDataObject s3DownloadLinkDataObject = deformedAssetWWW.GetResponseJson<S3DownloadLinkDataObject>();
                            //correct url by removing duplicated forward slashes 
                            string s3DownloadLinkDataObject_correctedUrl = s3DownloadLinkDataObject.location.Replace("\\", "");
                            //cachedpath = WWWCachedRequest.GetCachePath(s3DownloadLinkDataObject_correctedUrl, null, -1); ;
                            //Debug.Log("Corrected S3 location: " + s3DownloadLinkDataObject_correctedUrl);
                            //make a request to download from the provided location
                            WWWResponseRequest wwwRequestS3 = new WWWResponseRequest(coroutineManager,
                                                                                     requestHeader,
                                                                                     s3DownloadLinkDataObject_correctedUrl,
                                                                                     true,
                                                                                     null,
                                                                                     false);

                            //wait for response
                            yield return wwwRequestS3;

                            if (wwwRequestS3.Error != null)
                            {
                                //Debug.LogError("Error downloading deformed vertex from S3. " + wwwRequestS3.Error);
                                deformedAssetWWW.DeleteCacheOfRequest();
                                HandleError(wwwRequestS3, onFailureDelegate);
                            }
                            else
                            {
                                Debug.Log("Success - downloaded deformed vertex from S3");
                                uploadWWW.SaveRequestToCache();
                                deformedAssetWWW.SaveRequestToCache();
                                //load response and continue with the action delegates
                                onSuccessDelegate(wwwRequestS3.GetResponseBytes());
                            }
                        }
                    }
                }
                else if (baseResponse != null && !baseResponse.IsSuccess)
                {
                    //if the download fails then the first request must be deleted or else it will point to an expired s3 link which is not on cache
                    uploadWWW.DeleteCacheOfRequest();
                    onFailureDelegate(new System.Exception(baseResponse.msg));
                }
                else
                {
                    onFailureDelegate(new System.Exception("Server response could not be processed. You may be behind a proxy or captive portal.\nPlease check if you can visit www.mydidimo.com on your browser"));
                }
            }
        }

    }
}
