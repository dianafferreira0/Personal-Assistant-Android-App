using Didimo.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    public partial class ErrorOverlay : ErrorOverlayBase
    { }

    public class ErrorOverlayBase : MonoBehaviourSingleton<ErrorOverlay>
    {
        protected GameObject errorMenuGameObject;
        protected Text errorMenuText;
        protected Button okButton;

        protected virtual void Init()
        {
            errorMenuGameObject = Instantiate(Resources.Load<GameObject>("ErrorMenu"));
            errorMenuGameObject.transform.SetParent(transform);
            errorMenuText = errorMenuGameObject.transform.FindRecursive("Text").GetComponent<Text>();
            okButton = errorMenuGameObject.transform.FindRecursive("OkButton").GetComponent<Button>();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)errorMenuGameObject.transform);
        }

        public virtual void ShowError(string errorMessage, System.Action onOkPressedDelegate = null)
        {
            LoadingOverlay.Instance.Hide();

            if (errorMenuGameObject == null)
            {
                Init();
            }

            if (onOkPressedDelegate != null)
            {
                okButton.transform.GetComponentInChildren<Text>().text = "OK";
                okButton.onClick.AddListener(
                    () =>
                    {
                        CleanupListeners();
                        onOkPressedDelegate();
                    });
            }

            errorMenuGameObject.gameObject.SetActive(true);
            errorMenuText.text = errorMessage;
        }

        public virtual void ShowError(string errorMessage, string buttonMessage, System.Action onOkPressedDelegate = null)
        {
            LoadingOverlay.Instance.Hide();

            if (errorMenuGameObject == null)
            {
                Init();
            }

            if (onOkPressedDelegate != null)
            {
                okButton.transform.GetComponentInChildren<Text>().text = buttonMessage;
                okButton.onClick.AddListener(
                    () =>
                    {
                        CleanupListeners();
                        onOkPressedDelegate();
                    });
            }

            errorMenuGameObject.gameObject.SetActive(true);
            errorMenuText.text = errorMessage;
        }

        public virtual void CleanupListeners()
        {
            if (okButton != null)
                okButton.onClick.RemoveAllListeners();
        }
    }
}