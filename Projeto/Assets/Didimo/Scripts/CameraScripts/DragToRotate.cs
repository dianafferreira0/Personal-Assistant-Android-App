using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Didimo.CameraScripts
{
    /// <summary>
    /// This MonoBehaviour will capture mouse drags and rotate the object it is attached to, around the X and Y axis.
    /// Will ignore drags that are performed above EventSystem objects (interactible UI).
    /// </summary>
    public class DragToRotate : MonoBehaviour
    {
        /// <summary>
        /// The max rotation the component will around the X and Y axis. If set to -1, the rotation will not be limited. Accepts values in the range of [-1, 180].
        /// </summary>
        [SerializeField]
        Vector2 rotationLimit = new Vector2(0, 45);
        /// <summary>
        /// The object will rotate in an ammount of degrees equal to the mouse drag in pixels, times this modifier.
        /// Default value is 0.2f.
        /// </summary>
        [SerializeField]
        float rotationSpeed = 0.2f;
        /// <summary>
        /// Should the rotation around the X axis be inverted.
        /// </summary>
        [SerializeField]
        bool invertX = false;
        /// <summary>
        /// Should the rotation around the Y axis be inverted.
        /// </summary>
        [SerializeField]
        bool invertY = false;
        bool isDragging = false;
        Vector3 dragOrigin;
        Quaternion startRotation;
        Quaternion resetRotation;

        [ExecuteInEditMode]
        void OnValidate()
        {
            rotationLimit.x = Mathf.Max(-1, rotationLimit.x);
            rotationLimit.y = Mathf.Max(-1, rotationLimit.y);
        }

        // Use this for initialization
        void Start()
        {
            resetRotation = gameObject.transform.localRotation;
        }

        void LateUpdate()
        {
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            //Don't rotate if we are interacting with the UI
            if (!IsPointerOverUIObject() || isDragging)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    dragOrigin = Input.mousePosition;
                    startRotation = transform.localRotation;
                    isDragging = true;
                }
                if (Input.GetMouseButton(0) && isDragging)
                {
                    Vector2 positionChange = new Vector2(Input.mousePosition.x - dragOrigin.x, Input.mousePosition.y - dragOrigin.y);

                    gameObject.transform.localRotation = startRotation;

                    float rotationX = (invertY ? positionChange.y : -positionChange.y) * rotationSpeed;
                    //Debug.Log(rotationX);

                    if (rotationLimit.x >= 0)
                    {
                        rotationX = Mathf.Clamp(rotationX, -rotationLimit.x * 2f, rotationLimit.x * 2f);
                    }

                    float rotationY = (invertX ? positionChange.x : -positionChange.x) * rotationSpeed;
                    if (rotationLimit.y >= 0)
                    {
                        rotationY = Mathf.Clamp(rotationY, -rotationLimit.y * 2f, rotationLimit.y * 2f);
                    }
                    gameObject.transform.Rotate(
                        rotationX,
                        rotationY,
                        0);

                    Vector3 rot = gameObject.transform.localRotation.eulerAngles;

                    if (rot.y > 180)
                    {
                        rot.y -= 360;
                    }
                    if (rot.x > 180)
                    {
                        rot.x -= 360;
                    }

                    if (rotationLimit.x >= 0)
                    {
                        if (rot.x > rotationLimit.x)
                        {
                            rot.x = rotationLimit.x;
                        }
                        else if (rot.x < -rotationLimit.x)
                        {
                            rot.x = -rotationLimit.x;
                        }
                    }

                    if (rotationLimit.y >= 0)
                    {
                        if (rot.y > rotationLimit.y)
                        {
                            rot.y = rotationLimit.y;
                        }
                        else if (rot.y < -rotationLimit.y)
                        {
                            rot.y = -rotationLimit.y;
                        }
                    }

                    rot.z = 0;
                    gameObject.transform.localRotation = Quaternion.Euler(rot);
                }
            }
        }

        private bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public void ResetPosition()
        {
            gameObject.transform.localRotation = resetRotation;
        }
    }
}