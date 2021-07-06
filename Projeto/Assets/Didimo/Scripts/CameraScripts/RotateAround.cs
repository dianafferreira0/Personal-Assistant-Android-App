using UnityEngine;

namespace Didimo.CameraScripts
{
    /// <summary>
    /// This MonoBehaviour will make the GameObject it is attached to rotate around the <see cref="target"/> Transform.
    /// </summary>
    public class RotateAround : MonoBehaviour
    {
        /// <summary>
        /// The target to rotate around.
        /// </summary>
        public Transform target;
        /// <summary>
        /// The rotation speed, in angles per second.
        /// </summary>
        public float rotationAnglePerSec;

        void Start()
        {
            if (target == null)
            {
                Debug.LogError("Please define a target to rotate around by setting the 'target' property in the inspector. This script will be disabled.");
                enabled = false;
            }
        }

        void LateUpdate()
        {
            transform.RotateAround(target.position, Vector3.up, rotationAnglePerSec * Time.deltaTime);
        }
    }
}