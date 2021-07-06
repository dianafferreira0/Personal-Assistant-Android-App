using UnityEngine;
using UnityEngine.EventSystems;

namespace Didimo.Menu.Utils
{
    public class ResizePanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {

        public Vector2 minSize;
        public Vector2 maxSize;
        public RectTransform resizeIcon;

        private RectTransform rectTransform;
        private Vector2 startPointerPosition;
        private Vector2 startSize;
        private Vector2 currentPointerPosition;
        private bool resizing;

        void Awake()
        {
            rectTransform = transform.parent.GetComponent<RectTransform>();
            resizeIcon.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData data)
        {
            rectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out startPointerPosition);
            startSize = rectTransform.sizeDelta;
            resizing = true;
        }

        public void OnDrag(PointerEventData data)
        {
            if (rectTransform == null)
                return;

            Vector2 sizeDelta = startSize;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out currentPointerPosition);
            Vector2 resizeValue = currentPointerPosition - startPointerPosition;

            sizeDelta += new Vector2(resizeValue.x, -resizeValue.y);
            sizeDelta = new Vector2(
                Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),
                Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y)
                );

            rectTransform.sizeDelta = sizeDelta;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            resizeIcon.gameObject.SetActive(true);
            Cursor.visible = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!resizing)
            {
                resizeIcon.gameObject.SetActive(false);
                Cursor.visible = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            resizeIcon.gameObject.SetActive(false);
            resizing = false;
            Cursor.visible = true;
        }
    }
}