using Didimo.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    public partial class LoadingOverlay : LoadingOverlayBase
    {
    }

    public class LoadingOverlayBase : MonoBehaviourSingleton<LoadingOverlay>
    {
        protected GameObject loadingMenuGameObject;
        protected Text loadingText;
        protected Text progressText;


        protected System.Action onCancelDelegate;
        public string defaultLoadingText = "Loading...";

        protected string loadingMenuPrefabName = "LoadingMenu";

        protected virtual void Init()
        {
            Init(loadingMenuPrefabName);
        }
        protected virtual void Init(string prefabName)
        {
            loadingMenuGameObject = Instantiate(Resources.Load<GameObject>(prefabName));
            loadingMenuGameObject.transform.SetParent(transform);

            loadingText = loadingMenuGameObject.transform.FindRecursive("Text").GetComponent<Text>();
            progressText = loadingMenuGameObject.transform.FindRecursive("ProgressText").GetComponent<Text>();
            loadingMenuGameObject.transform.FindRecursive("CancelButton").GetComponent<Button>().onClick.AddListener(Cancel);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)loadingMenuGameObject.transform);

        }

        public virtual void ShowProgress(float progress)
        {
            progressText.text = progress.ToString("0") + " %";
        }

        public void ShowPhasedProgress(string phase_label, float progress)
        {
            //progressText.text = phase_label+ "\r\n" + progress.ToString("0") + "%";
            progressText.text = phase_label + "\r\n";
            if ((int)progress < 10)
                progressText.text = progressText.text + " ";
            else if ((int)progress < 100)
                progressText.text = progressText.text + "  ";
            progressText.text = progressText.text + progress.ToString("0") + "%";
        }

        public virtual void ShowLoadingMenu(System.Action onCancelDelegate, string text = null)
        {
            if (loadingMenuGameObject == null)
            {
                Init();
            }

            if (loadingText != null)
            {
                if (text == null)
                {
                    loadingText.text = defaultLoadingText;
                }
                else
                {
                    loadingText.text = text;
                }
            }

            this.onCancelDelegate = onCancelDelegate;
            loadingMenuGameObject.SetActive(true);
            progressText.text = "";
        }


        public void Cancel()
        {
            onCancelDelegate();
        }

        public virtual void Hide()
        {
            if (loadingMenuGameObject == null)
            {
                Init();
            }
            onCancelDelegate = null;
            loadingMenuGameObject.SetActive(false);
        }
    }
}