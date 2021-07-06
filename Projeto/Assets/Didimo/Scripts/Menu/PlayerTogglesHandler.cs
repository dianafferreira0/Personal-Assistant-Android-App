using Didimo.DidimoManagement;
using Didimo.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    public class PlayerTogglesHandler : MonoBehaviour
    {

        [SerializeField]
        GameObject idlesPlayerBtn = null;
        [SerializeField]
        GameObject speechPlayerBtn = null;
        [SerializeField]
        GameObject mocapPlayerBtn = null;
        [SerializeField]
        GameObject hairConfigBtn = null;
        [SerializeField]
        GameObject hairColorConfigBtn = null;
        [SerializeField]
        GameObject eyesConfigBtn = null;


        [SerializeField]
        GameObject idlesPanel = null;
        [SerializeField]
        GameObject speechPanel = null;
        [SerializeField]
        public GameObject mocapPanel = null;
        [SerializeField]
        public GameObject eyesConfigPanel = null;
        [SerializeField]
        GameObject hairConfigPanel = null;
        [SerializeField]
        public GameObject hairColorConfigPanel = null;

        public void OnIdlesToggleEvent(Toggle toggle)
        {
            idlesPanel.SetActive(toggle.isOn);
        }

        public void OnSpeechToggleEvent(Toggle toggle)
        {
            speechPanel.SetActive(toggle.isOn);
        }

        public void OnMocapToggleEvent(Toggle toggle)
        {
            mocapPanel.SetActive(toggle.isOn);
        }

        public void OnEyesToggleEvent(Toggle toggle)
        {
            eyesConfigPanel.SetActive(toggle.isOn);
        }

        public void OnHairToggleEvent(Toggle toggle)
        {
            hairConfigPanel.SetActive(toggle.isOn);
        }

        public void OnHairColorToggleEvent(Toggle toggle)
        {
            hairColorConfigPanel.SetActive(toggle.isOn);
        }

        //
        public void HideAll()
        {
            speechPanel.SetActive(false);
            hairColorConfigPanel.SetActive(false);
            mocapPanel.SetActive(false);
            eyesConfigPanel.SetActive(false);
            hairConfigPanel.SetActive(false);
            idlesPanel.SetActive(false);
        }

        //////////
        public void DisableSpeechOption()
        {
            speechPanel.SetActive(false);
            speechPlayerBtn.SetActive(false);
        }
        public void EnableSpeechOption()
        {
            speechPanel.SetActive(false);
            speechPlayerBtn.SetActive(true);
        }

        public void DisableHairColorOption()
        {
            hairColorConfigPanel.SetActive(false);
            hairColorConfigBtn.SetActive(false);
        }
        public void EnableHairColorOption()
        {
            hairColorConfigPanel.SetActive(false);
            hairColorConfigBtn.SetActive(true);
        }

        public void DisableMocapOption()
        {
            mocapPanel.SetActive(false);
            mocapPlayerBtn.SetActive(false);
        }
        public void EnableMocapOption()
        {
            mocapPanel.SetActive(false);
            mocapPlayerBtn.SetActive(true);
        }

        public void DisableEyesConfigOption()
        {
            eyesConfigPanel.SetActive(false);
            eyesConfigBtn.SetActive(false);
        }
        public void EnableEyesConfigOption()
        {
            eyesConfigPanel.SetActive(false);
            eyesConfigBtn.SetActive(true);
        }

        public void EnableHairOption()
        {
            hairConfigPanel.SetActive(false);
            hairConfigBtn.SetActive(true);
        }

        public void EnableIdlesPlayer(bool isExpressionSupported)
        {
            idlesPlayerBtn.SetActive(true);
            idlesPanel.SetActive(false);

            if (isExpressionSupported) //full range of motion supported by the rig
            {
                foreach (Toggle t in idlesPanel.transform.GetComponentsInChildren<Toggle>(true))
                {
                    t.interactable = true;
                }
            }
            else //disable poses that won't work correctly without the full rig: 1,2,3,
            {
                int i = 0;
                foreach (Toggle t in idlesPanel.transform.GetComponentsInChildren<Toggle>(true))
                {
                    if (i == 0 || i == 4 || i == 5) //only supports a few basic facial movements that resemble expressions...
                    {
                        t.interactable = true;
                        t.transform.GetComponentInChildren<Image>(true).color = new Color32(255, 255, 255, 255);
                    }
                    else
                    {
                        t.interactable = false; //disable
                        t.transform.GetComponentInChildren<Image>(true).color = new Color32(0, 0, 0, 120);
                    }

                    if (i == 0)
                        t.SetIsOnWithoutNotify(true);
                    i++;
                }
            }
        }
    }
}
