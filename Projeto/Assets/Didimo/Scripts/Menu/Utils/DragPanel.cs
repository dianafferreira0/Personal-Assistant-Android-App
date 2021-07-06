using UnityEngine;
using UnityEngine.EventSystems;

namespace Didimo.Menu.Utils
{
    public class DragPanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {

        private Vector3 initialWindowPos;
        private Vector2 initialMousePos;
        private RectTransform canvasRectTransform;
        private RectTransform panelRectTransform;
        bool dragging;

        void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRectTransform = canvas.transform as RectTransform;
                panelRectTransform = transform as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.pointerCurrentRaycast.gameObject == gameObject)
            {
                initialWindowPos = panelRectTransform.localPosition;
                initialMousePos = data.position;
                dragging = true;
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (panelRectTransform == null || !dragging)
                return;

            Vector2 pointerPostion = ClampToWindow(data);

            Vector2 offset = pointerPostion - initialMousePos;
            Vector3 offsetVec3 = new Vector3(offset.x, offset.y, 0);

            panelRectTransform.localPosition = offsetVec3 + initialWindowPos;
        }

        Vector2 ClampToWindow(PointerEventData data)
        {
            Vector2 rawPointerPosition = data.position;

            Vector3[] canvasCorners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(canvasCorners);

            float clampedX = Mathf.Clamp(rawPointerPosition.x, canvasCorners[0].x, canvasCorners[2].x);
            float clampedY = Mathf.Clamp(rawPointerPosition.y, canvasCorners[0].y, canvasCorners[2].y);

            Vector2 newPointerPosition = new Vector2(clampedX, clampedY);
            return newPointerPosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
        }
    }
}