using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Didimo.Menu.Utils
{
    public class DoubleClickableButton : Button, IPointerClickHandler
    {
        public UnityAction onDoubleClickAction;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 1)
            {
                onClick.Invoke();
            }
            if (eventData.clickCount == 2)
            {
                if (onDoubleClickAction != null)
                {
                    onDoubleClickAction();
                }
            }
        }
    }
}