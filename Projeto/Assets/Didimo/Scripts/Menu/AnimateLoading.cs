using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    [RequireComponent(typeof(Image))]
    public class AnimateLoading : MonoBehaviour
    {
        public float animationTime = 1;
        //in seconds
        public Sprite[] sprites;
        private Image image;
        public Text progressText;
        public Text titleText;

        void Start()
        {
            image = gameObject.GetComponent<Image>();
            image.color = Color.white;
        }

        // Update is called once per frame
        void Update()
        {

            float time = Time.realtimeSinceStartup / (animationTime / sprites.Length);
            int frame = (int)time % sprites.Length;

            image.sprite = sprites[frame];
        }

        public void SetProgressString(string progressString)
        {

            progressText.text = progressString;
        }

        public void SetTitle(string titleString)
        {

            titleText.text = titleString;
        }
    }
}