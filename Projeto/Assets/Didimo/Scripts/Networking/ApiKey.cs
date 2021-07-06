using Didimo.Networking.Header;
using UnityEngine;
using Didimo.Utils.Coroutines;
using Didimo.Menu;

namespace Didimo.Networking
{
    public class ApiKey : MonoBehaviour
    {
        /// <summary>
        /// Should this script configure the ServicesRequests class to use APIKey. Set to false if you are logging somewhere else, or using another set of apiKey and secretKey.
        /// </summary>
        public static bool shouldConfigure = true;
        public string apiKey;
        public string secretKey;

        GameCoroutineManager coroutineManager;

        // Use this for initialization
        void Awake()
        {
            if (shouldConfigure)
            {
                ///Make our requests have API Key authentication
                ServicesRequests.GameInstance.ConfigureForAPIKeyHeader(apiKey, secretKey.Trim());
                ValidateCookies(); //to get the user profile tier level needed to generate didimos
            }
        }

        // Use this for initialization
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ValidateCookies();
            }
        }

        void ValidateCookies()
        {
            if (coroutineManager == null)
            {
                coroutineManager = new GameCoroutineManager();
            }

            ServicesRequests.GameInstance.Profile(
                    coroutineManager,
                    (profile) =>
                    {
                    //Success, do nothing
                    },
                    exception =>
                    {
                        ErrorOverlay.Instance.ShowError(exception.Message,
                                () =>
                                {
                                    Debug.Log("exception: " + exception);
                                    LoadingOverlay.Instance.Hide();
                                });
                    });
        }

    }
}