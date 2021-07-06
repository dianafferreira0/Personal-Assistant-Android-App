using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu.FileBrowser
{
    public class IconImageTextLayout : MonoBehaviour
    {
        [SerializeField]
        Text icon = null;
        [SerializeField]
        RawImage image = null;
        [SerializeField]
        Text description = null;

        Vector2 imageMaxSize;

        void Start()
        {

            imageMaxSize = image.rectTransform.sizeDelta;
        }

        public void Configure(char icon, string description)
        {
            this.icon.text = icon.ToString();
            this.description.text = description;
            image.gameObject.SetActive(false);
            this.icon.gameObject.SetActive(true);
        }

        public void Configure(Texture texture, string description)
        {
            float aspect = texture.width / (float)texture.height;
            Vector2 size = new Vector2(Mathf.Min(imageMaxSize.x, texture.width), Mathf.Min(imageMaxSize.y, texture.height));

            if (aspect > 1)
            {
                size.y = size.x / aspect;
            }
            else if (aspect < 1)
            {
                size.x = size.y / aspect;
            }

            image.rectTransform.sizeDelta = size;
            image.texture = texture;
            this.description.text = description;
            image.gameObject.SetActive(true);
            icon.gameObject.SetActive(false);
        }
    }
}