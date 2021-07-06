using UnityEngine;

namespace Didimo.Menu
{
    public class HideAtStart : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {

            gameObject.SetActive(false);
        }
    }
}
