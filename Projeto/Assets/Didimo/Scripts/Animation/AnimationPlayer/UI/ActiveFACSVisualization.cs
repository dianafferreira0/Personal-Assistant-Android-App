using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Animation.AnimationPlayer.UI
{
    public class ActiveFACSVisualization : MonoBehaviour
    {
        public bool animatingManually = false;
        public RealtimeRig realtimeRig;
        public GameObject templatePanel;
        protected List<Slider> sliders;
        protected List<string> facNames;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (templatePanel == null)
            {
                Debug.LogError("Please assign templatePanel object. Disabling behaviour.");
                enabled = false;
                return;
            }
            if (realtimeRig == null)
            {
                Debug.LogError("Please assign realtimeRig object. Disabling behaviour.");
                enabled = false;
            }

            Slider slider = templatePanel.GetComponentInChildren<Slider>();
            if (slider == null)
            {
                Debug.LogError("template panel doesn't have a child with a Slider component. Disabling behaviour.");
                enabled = false;
            }

            Text text = templatePanel.GetComponentInChildren<Text>();
            if (text == null)
            {
                Debug.LogError("template panel doesn't have a child with a Text component. Disabling behaviour.");
                enabled = false;
            }

            if (enabled)
            {
                SetupGUI();
            }
        }

        void SetupGUI()
        {
            facNames = realtimeRig.GetSupportedFACS();
            sliders = new List<Slider>();

            for (int i = 0; i < facNames.Count; i++)
            {
                GameObject sliderPanel = Instantiate(templatePanel, templatePanel.transform.parent);
                Slider slider = sliderPanel.GetComponentInChildren<Slider>();
                sliders.Add(slider);
                string facName = facNames[i];
                slider.onValueChanged.AddListener(delegate
                {
                    if (animatingManually)
                    {
                        realtimeRig.SetBlendshapeWeightsForFac(facName, slider.value);
                    }
                });
                sliderPanel.GetComponentInChildren<Text>().text = facNames[i];

            }
            templatePanel.SetActive(false);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            for (int i = 0; i < sliders.Count; i++)
            {
                sliders[i].interactable = animatingManually;
                if (!animatingManually)
                {
                    sliders[i].value = realtimeRig.GetWeightForFAC(facNames[i]);
                }
            }
        }
    }
}
