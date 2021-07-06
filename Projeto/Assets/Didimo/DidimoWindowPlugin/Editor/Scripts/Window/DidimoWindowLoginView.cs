using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using UnityEditor;
using UnityEngine;

namespace Didimo.Editor.Window
{
    /// <summary>
    /// The purpose of this class is to draw the login screen for the Didimo Window. Uses GUILayout/EditorGUILayout.
    /// </summary>
    [System.Serializable]
    public class DidimoWindowLoginView : ScriptableObject
    {
        const int loginFieldsWidth = 80;
        enum State
        {
            Login,
            LoggingIn
        }

        System.Action didLoginDelegate;
        System.Action didClickRegisterDelegate;

        // GUIStyles break if they get serialized
        [System.NonSerialized]
        GUIStyle textFieldStyle;
        [System.NonSerialized]
        GUIStyle registerButtonStyle;
        [SerializeField]
        State state;

        [SerializeField]
        LoginDataObject loginDataObject;

        /// <summary>
        /// DidimoWindowLoginView constructor.
        /// </summary>
        /// <param name="didLoginDelegate">The delegate to be called when the user successfully logs in.</param>
        /// <param name="didClickRegisterDelegate">The Delegateto be called when the user clicks the register button.</param>
        public void Init(System.Action didLoginDelegate, System.Action didClickRegisterDelegate)
        {
            this.didLoginDelegate = didLoginDelegate;
            this.didClickRegisterDelegate = didClickRegisterDelegate;
        }

        private void OnEnable()
        {
            state = State.Login;
            if (loginDataObject == null)
            {
                loginDataObject = new LoginDataObject();
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
                registerButtonStyle = new GUIStyle(EditorStyles.label);
                registerButtonStyle.normal.textColor = new Color(92f/255f, 237f/255f, 150f/255f);
                registerButtonStyle.active.textColor = new Color(76f/255f, 187f/255f, 23f/255f);
            }

            switch (state)
            {
                case State.Login:
                    DrawLogin();
                    break;
                case State.LoggingIn:
                    DrawLoggingIn();
                    break;
            }
        }

        void DrawLogin()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("e-mail", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));
            loginDataObject.email = EditorGUILayout.TextField(loginDataObject.email, textFieldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("password", EditorStyles.boldLabel, GUILayout.Width(loginFieldsWidth));
            loginDataObject.password = EditorGUILayout.PasswordField(loginDataObject.password, textFieldStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Login", GUILayout.ExpandWidth(false)))
            {
                if (string.IsNullOrEmpty(loginDataObject.email) || string.IsNullOrEmpty(loginDataObject.password))
                {
                    EditorUtility.DisplayDialog("Login failed", "Please fill the email and password fields.", "OK");
                }
                else
                {
                    PerformLogin();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Don't have an account?");
            if (GUILayout.Button("Register", registerButtonStyle))
            {
                didClickRegisterDelegate.Invoke();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void DrawLoggingIn()
        {
            EditorUtility.DisplayProgressBar("Logging in...", "", 1);
            EditorGUI.BeginDisabledGroup(true);
            DrawLogin();
            EditorGUI.EndDisabledGroup();
        }

        public void PerformLogin(LoginDataObject loginDataObject)
        {
            this.loginDataObject = loginDataObject;
            PerformLogin();
        }

        void PerformLogin()
        {
            state = State.LoggingIn;
            ServicesRequests.EditorInstance.Login(new EditorCoroutineManager(),
                loginDataObject,
                () =>
                {
                    EditorUtility.ClearProgressBar();
                    state = State.Login;
                    didLoginDelegate.Invoke();
                },
            exception =>
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Login failed", exception.Message, "OK");
                state = State.Login;
            });
        }
    }
}