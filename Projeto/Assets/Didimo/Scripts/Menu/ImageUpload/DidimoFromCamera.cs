using Didimo.DidimoManagement;
using Didimo.Networking;
using Didimo.Utils.Coroutines;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu.ImageUpload
{
    /// <summary>
    /// This MonoBehaviour is responsible to render the image feed from a WebCam into a Texture, and to create a Didimo from it.
    /// </summary>
    public class DidimoFromCamera : DidimoManagerMonoBehaviour
    {

        public class Camera
        {
            public WebCamDevice device;
            public WebCamTexture texture;

            public Camera(WebCamDevice device, WebCamTexture texture)
            {
                this.device = device;
                this.texture = texture;
            }
        }
        public GameObject SnapPhoto;
        public DidimoMenuHandler mainMenu;

        /// <summary>
        /// The <see cref="RawImage"/> component we will use to render the WebCam feed.
        /// </summary>
        [SerializeField]
        RawImage rawCameraImage = null;
        /// <summary>
        /// The button used to cycle through cameras. Will only be shown if at least two cameras are available.
        /// </summary>
        [SerializeField]
        GameObject nextCameraButton = null;

        List<Camera> cameras;
        Camera activeCamera;

        // Image rotation
        Vector3 rotationVector = new Vector3(0f, 0f, 0f);

        // Image uvRect
        Rect defaultRect = new Rect(0f, 0f, 1f, 1f);
        Rect fixedRect = new Rect(0f, 1f, 1f, -1f);

        // Image Parent's scale
        Vector3 defaultScale = new Vector3(1f, 1f, 1f);
        //Vector3 fixedScale = new Vector3(-1f, -1f, 1f);

        private void OnEnable()
        {
            if (cameras == null)
            {
                cameras = new List<Camera>();
            }
        }

        // Set the device camera to use and start it
        public void SetActiveCamera(Camera cameraToUse)
        {
            if (activeCamera != null)
            {
                activeCamera.texture.Stop();
            }

            activeCamera = cameraToUse;

            rawCameraImage.texture = activeCamera.texture;
            //rawCameraImage.material.mainTexture = activeCamera.texture;
            activeCamera.texture.Play();

            if (activeCamera.device.isFrontFacing)
            {
                Debug.Log("Is front facing");
            }
            else
            {
                Debug.Log("Isn't front facing");
            }


            if (activeCamera.texture.videoVerticallyMirrored)
            {
                Debug.Log("Is mirrored vertically");
            }
            else
            {
                Debug.Log("Isn't mirrored vertically");
            }
        }

        // Switch between the device's front and back camera
        public void SwitchToNextCamera()
        {
            int currentCameraIndex = cameras.IndexOf(activeCamera);
            int nextCameraIndex = (currentCameraIndex + 1) % cameras.Count;
            if (currentCameraIndex != nextCameraIndex)
            {
                SetActiveCamera(cameras[nextCameraIndex]);
            }
        }

        // Make adjustments to image every frame to be safe, since Unity isn't
        // guaranteed to report correct data as soon as device camera is started
        void Update()
        {
            if (rawCameraImage.isActiveAndEnabled && activeCamera != null)
            {
                // Skip making adjustment for incorrect camera data
                if (activeCamera.texture.width < 100)
                {
                    Debug.Log("Still waiting another frame for correct info...");
                    return;
                }

                // TODO: get this value to check if camera is mirrored vertically, instead of using "fixedScale" with an inverted y scale
                //if (activeCamera.texture.videoVerticallyMirrored)
                //{
                //    rawCameraImage.transform.localScale = new Vector3(1, -1, 1);
                //}
                //else
                //{
                //    rawCameraImage.transform.localScale = new Vector3(1, 1, 1);
                //}

                // Rotate image to show correct orientation 
                rotationVector.z = -activeCamera.texture.videoRotationAngle;
                rawCameraImage.rectTransform.localEulerAngles = rotationVector;

                // Unflip if vertically flipped
                rawCameraImage.uvRect =
                activeCamera.texture.videoVerticallyMirrored ? fixedRect : defaultRect;

                // Mirror front-facing camera's image horizontally to look more natural
                //rawCameraImage.rectTransform.localScale =
                //activeCamera.device.isFrontFacing ? fixedScale : defaultScale;

                //if (activeCamera == cameras.First())
                //{
                rawCameraImage.rectTransform.localScale = defaultScale;
                //}
                //else
                //{
                //rawCameraImage.rectTransform.localScale = fixedScale;
                //}
            }
        }

        private IEnumerator Initialize()
        {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            }

            // Check for device cameras
            if (WebCamTexture.devices.Length > 0)
            {
                cameras.Clear();
                foreach (WebCamDevice device in WebCamTexture.devices)
                {
                    WebCamTexture webCamTexture = new WebCamTexture(device.name);
                    webCamTexture.filterMode = FilterMode.Bilinear;

                    cameras.Add(new Camera(device, webCamTexture));
                }
            }

            nextCameraButton.SetActive(cameras.Count > 1);
        }

        Texture2D FlipTextureVertically(Texture2D original, bool upSideDown = true)
        {

            Texture2D flipped = new Texture2D(original.width, original.height);

            int xN = original.width;
            int yN = original.height;


            for (int i = 0; i < xN; i++)
            {
                for (int j = 0; j < yN; j++)
                {
                    if (upSideDown)
                    {
                        flipped.SetPixel(j, xN - i - 1, original.GetPixel(j, i));
                    }
                    else
                    {
                        flipped.SetPixel(xN - i - 1, j, original.GetPixel(i, j));
                    }
                }
            }
            flipped.Apply();

            return flipped;
        }

        /// <summary>
        /// Rotates a texture 90 degrees to either side.
        /// </summary>
        /// <param name="original">The texture to be rotated.</param>
        /// <param name="rotateRight">If true, rotates the photo 90 degrees to the right. Otherwise, rotates 90 degress to the left.</param>
        /// <returns>The rotated photo.</returns>
        Texture2D RotateTexture(Texture2D original, bool rotateRight = true)
        {
            Texture2D flipped = new Texture2D(original.height, original.width);

            int xN = original.width;
            int yN = original.height;

            for (int i = 0; i < xN; i++)
            {
                if (rotateRight)
                {
                    flipped.SetPixels(0, i, yN, 1, original.GetPixels(i, 0, 1, yN));
                }
                else
                {
                    flipped.SetPixels(0, yN - i - 1, yN, 1, original.GetPixels(i, 0, 1, yN));
                }
            }
            flipped.Apply();

            return flipped;
        }

        public void TakeAPhoto()
        {
            SnapPhoto.SetActive(false);
            Texture2D snap = new Texture2D(activeCamera.texture.width, activeCamera.texture.height);
            snap.SetPixels(activeCamera.texture.GetPixels());

            snap.Apply();

            activeCamera.texture.Stop();

            GameCoroutineManager coroutineManager = new GameCoroutineManager();

            ServicesRequests.GameInstance.CreateDidimoCheckProgress(
                coroutineManager,
                snap,
                "",
                (didimoKey) =>
                {
                    LoadingOverlay.Instance.ShowLoadingMenu(() =>
                    {
                        coroutineManager.StopAllCoroutines();
                    }, "Downloading...");


                    ServicesRequests.GameInstance.GetDidimoDefinitionModel(
                        coroutineManager,
                        didimoKey,
                        (didimoDefinitionModel) =>
                        {
                            mainMenu.DidimoImporter.ImportDidimo(coroutineManager, didimoKey, didimoDefinitionModel.meta,
                                didimoGameObject =>
                                {
                                    Debug.Log("Didimo loading is complete!");
                                    LoadingOverlay.Instance.Hide(); 
                                    if (mainMenu != null)
                                    {
                                        mainMenu.HideMenu();
                                        mainMenu.InitPlayerPanel(didimoGameObject, didimoKey, didimoDefinitionModel.meta); 
                                    }
                                },
                                exception =>
                                {
                                    ErrorOverlay.Instance.ShowError(exception.Message);
                                }
                            );
                        },
                        exception =>
                        {
                            ErrorOverlay.Instance.ShowError(exception.Message);
                        }
                    );
                },
                exception =>
                {
                    ErrorOverlay.Instance.ShowError(exception.Message);
                },
                progress =>
                {
                    LoadingOverlay.Instance.ShowProgress(progress);
                });

            LoadingOverlay.Instance.ShowLoadingMenu(() =>
            {
                StartCapture();
                coroutineManager.StopAllCoroutines();
            }, "Processing...");
        }

        public void StopCapture()
        {

            if (activeCamera != null)
            {
                activeCamera.texture.Stop();
                activeCamera = null;
            }

            cameras.Clear();
        }

        public void StartCapture()
        {
            StartCoroutine(StartCaptureAsync());
        }

        IEnumerator StartCaptureAsync()
        {
            yield return StartCoroutine(Initialize());

            if (cameras.Count > 0)
            {
                if (Application.HasUserAuthorization(UserAuthorization.WebCam))
                {
                    SetActiveCamera(cameras.First());
                }
                else
                {
                    ErrorOverlay.Instance.ShowError("Not authorized to use the webcam. Please change your camera preferences.");
                    //didimoMenuHandler.GoBack();
                }
            }
            else
            {
                ErrorOverlay.Instance.ShowError("Failed to initialize camera. No camera devices found.");
                //didimoMenuHandler.GoBack();
            }
        }
    }
}
