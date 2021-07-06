using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu.FileBrowser
{
    public class IconTextLayout : MonoBehaviour
    {
        [SerializeField]
        Text icon = null;
        [SerializeField]
        Text description = null;

        public void Configure(char icon, string description)
        {
            this.icon.text = icon.ToString();
            this.description.text = description;
        }
    }
}