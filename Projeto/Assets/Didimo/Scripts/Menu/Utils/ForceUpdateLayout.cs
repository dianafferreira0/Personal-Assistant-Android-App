using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu.Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class ForceUpdateLayout : MonoBehaviour
    {
        // Use this for initialization
        IEnumerator Start()
        {

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

            yield return null;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
    }
}