using System.Text.RegularExpressions;
using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using UnityEditor;
using UnityEngine;
using static Didimo.Editor.Window.DidimoWindow;

namespace Didimo.Editor.Window
{
    /// <summary>
    /// The purpose of this class is to draw the login screen for the Didimo Window. Uses GUILayout/EditorGUILayout.
    /// </summary>
    [System.Serializable]
    public class DidimoWindowSetupAuthKeyView : ScriptableObject
    {
        const int loginFieldsWidth = 80;

        System.Action<UserProfileDataObject> didConnectDelegate;

        // GUIStyles break if they get serialized
        [System.NonSerialized]
        GUIStyle textFieldStyle;
        [System.NonSerialized]
        GUIStyle textAreaStyle;
        [System.NonSerialized]
        GUIStyle connectButtonStyle;
        [SerializeField]
        public APIConnectionState state;

        [SerializeField]
        UserProfileDataObject profileDataObject;

        const string loginAuthKey = "didimo.loginkey";

        string APIConnectionKeyText//unprocessed text
        {
            get { return EditorPrefs.GetString(loginAuthKey, "copy the api key from the customer portal dashboard and paste it here"); }
            set { EditorPrefs.SetString(loginAuthKey, value); }
        }

        /// <summary>
        /// DidimoWindowSetupAuthKeyView constructor.
        /// </summary>
        /// <param name="didConnectDelegate">The delegate to be called when the user successfully connects with the API and obtains the user profile data object.</param>
        /// <param name="didClickRegisterDelegate">The Delegateto be called when the user clicks the register button.</param>
        public void Init(System.Action<UserProfileDataObject> didConnectDelegate)
        {
            this.didConnectDelegate = didConnectDelegate;
        }


        private void OnEnable()
        {
            state = APIConnectionState.NotSetup;
            if (profileDataObject == null)
            {
                profileDataObject = new UserProfileDataObject();
            }
        }

        /// <summary>
        /// Draw the login view.
        /// </summary>
        public void Draw()
        {
            if (textFieldStyle == null)
            {
                textFieldStyle = new GUIStyle(EditorStyles.textField);
                textFieldStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (textAreaStyle == null)
            {
                textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.wordWrap = true;
                textAreaStyle.fixedHeight = 150;
            }
            if (connectButtonStyle == null)
            {
                connectButtonStyle = new GUIStyle(EditorStyles.label);
                connectButtonStyle.normal.textColor = new Color(92f / 255f, 237f / 255f, 150f / 255f);
                connectButtonStyle.active.textColor = new Color(76f / 255f, 187f / 255f, 23f / 255f);
            }

            switch (state)
            {
                case APIConnectionState.NotSetup:
                    DrawConfigPanel();
                    break;
                case APIConnectionState.Disconnected:
                    DrawConfigPanel();
                    break;
                case APIConnectionState.Connecting:
                    DrawConnectingPanel();
                    break;
                case APIConnectionState.Connected:
                    //DrawLoggingIn();
                    break;
            }
        }

        void DrawConfigPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            /*
            EditorGUILayout.LabelField("API KEY", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));
            string apiKey = EditorGUILayout.TextField("insert api key here", textFieldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("SECRET", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));
            string secret = EditorGUILayout.TextField("secret goes here", textFieldStyle);
            */
            EditorGUILayout.LabelField("API KEY", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            APIConnectionKeyText = EditorGUILayout.TextArea(APIConnectionKeyText, textAreaStyle, GUILayout.Height(textAreaStyle.fixedHeight));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Connect", GUILayout.ExpandWidth(false)))
            {
                if (string.IsNullOrEmpty(APIConnectionKeyText) || string.IsNullOrEmpty(APIConnectionKeyText))
                {
                    EditorUtility.DisplayDialog("Empty data", "Please fill the API key and secret fields.", "OK");
                }
                else
                {
                    AttemptConnection(APIConnectionKeyText);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Don't have an account?");
            if (GUILayout.Button("Go to the Customer Portal", connectButtonStyle))
            {
                Application.OpenURL(ServicesRequestsConfiguration.DefaultConfig.CustomerPortalUrl);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void DrawConnectingPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("CONNECTING...", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth * 1.2f));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Please wait", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
            {
                CancelConnectionAttempt();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void AttemptConnection(string apiKeyText)
        {
            state = APIConnectionState.Connecting;

            string key = "";
            string secret = "";

            string[] result = apiKeyText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);// Regex.Split(apiKeyText, "\r\n|\r|\n");

            foreach (string line in result)
            {
                if (!string.IsNullOrEmpty(line) && (line.Contains("Key:") || line.Contains("Secret:")))
                {
                    string[] pair = Regex.Split(line, ":");
                    if (pair.Length == 2)
                    {
                        //get values and remove quotation marks
                        if (pair[0].Equals("Key"))
                            key = pair[1].Replace("\"", "").Trim();
                        else if (pair[0].Equals("Secret"))
                            secret = pair[1].Replace("\"", "").Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Parsing failed", "Could not read key and secret from the input text", "OK");
                state = APIConnectionState.Disconnected;
                return;
            }

            ///Make our requests have API Key authentication
            ServicesRequests.EditorInstance.ConfigureForAPIKeyHeader(key, secret);

            ServicesRequests.EditorInstance.Profile(new EditorCoroutineManager(),
                (profileDataObject) =>
                {
                    EditorUtility.ClearProgressBar();
                    state = APIConnectionState.Connected;
                    didConnectDelegate.Invoke(profileDataObject);
                },
            exception =>
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Login failed", exception.Message, "OK");
                state = APIConnectionState.Disconnected;
            });
        }

        void CancelConnectionAttempt()
        {
            new EditorCoroutineManager().StopAllCoroutines();
            state = APIConnectionState.Disconnected;
        }

    }
}
