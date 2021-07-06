using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    [RequireComponent(typeof(InputField))]
    public class TriggerButtonOnEnterKeyPress : MonoBehaviour
    {

        public Button button;

        InputField inputField;

        void Start()
        {

            inputField = GetComponent<InputField>();
        }

        void Update()
        {

            if (inputField.isFocused && inputField.text != "" && Input.GetKey(KeyCode.Return))
            {

                button.onClick.Invoke();
            }
        }
    }
}