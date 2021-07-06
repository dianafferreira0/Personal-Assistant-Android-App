using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils.Coroutines;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Didimo.Menu
{

    public class DidimoLogin : MonoBehaviour
    {
        [SerializeField]
        InputField apiKeyField = null;
        [SerializeField]
        InputField secretKeyField = null;
        [SerializeField]
        InputField email = null;
        [SerializeField]
        InputField password = null;
        [SerializeField]
        Text errorText = null;
        [SerializeField]
        GameObject errorMenu = null;
        [SerializeField]
        GameObject loadingMenu = null;
        [SerializeField]
        GameObject sceneSelectionMenu = null;
        [SerializeField]
        GameObject loginMenu = null;
        [SerializeField]
        GameObject apiKeyPanel = null;
        [SerializeField]
        GameObject userNamePanel = null;
        [SerializeField]
        Dropdown dropDown = null;
        EventSystem system;

        private void Start()
        {
            loginMenu.SetActive(true);
            sceneSelectionMenu.SetActive(false);
            loadingMenu.SetActive(false);
            errorMenu.SetActive(false);
            system = EventSystem.current;
            DropDownValueChanged();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Login();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

                if (next != null)
                {
                    InputField inputfield = next.GetComponent<InputField>();
                    if (inputfield != null)
                        inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret

                    system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                }
            }
        }

        public void DropDownValueChanged()
        {
            switch (dropDown.value)
            {
                case 0:
                    apiKeyPanel.SetActive(true);
                    userNamePanel.SetActive(false);
                    break;
                case 1:
                    apiKeyPanel.SetActive(false);
                    userNamePanel.SetActive(true);
                    break;
            }
        }

        public void UseApiKey()
        {
            ApiKey.shouldConfigure = false;
            if (string.IsNullOrEmpty(apiKeyField.text) || string.IsNullOrEmpty(secretKeyField.text))
            {
                errorText.text = "Please fill in your API Key and Secret Key";
                errorMenu.SetActive(true);
            }
            else
            {
                ServicesRequests.GameInstance.ConfigureForAPIKeyHeader(apiKeyField.text, secretKeyField.text);
                loginMenu.SetActive(false);
                sceneSelectionMenu.SetActive(true);
                loadingMenu.SetActive(false);
            }
        }

        public void Login()
        {
            ApiKey.shouldConfigure = false;
            if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(email.text))
            {
                errorText.text = "Please fill in your email and password";
                errorMenu.SetActive(true);
            }
            else
            {
                ServicesRequests.GameInstance.ConfigureForSessionHeader();
                LoginDataObject loginDataObject = new LoginDataObject(email.text, password.text);
                loadingMenu.SetActive(true);
                ServicesRequests.GameInstance.ConfigureForSessionHeader();
                ServicesRequests.GameInstance.Login(
                    new GameCoroutineManager(),
                         loginDataObject,
                         () =>
                         {
                             loginMenu.SetActive(false);
                             sceneSelectionMenu.SetActive(true);
                             loadingMenu.SetActive(false);
                         },
                     exception =>
					{
                         loadingMenu.SetActive(false);
						errorText.text = exception.Message;
                         errorMenu.SetActive(true);
                     });
            }
        }
    }
}