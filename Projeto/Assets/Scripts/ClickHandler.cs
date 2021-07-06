using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ClickHandler : MonoBehaviour
{
    public UnityEvent UpEvent;
    public UnityEvent downEvent;
    void OnMouseDown(){
        Debug.Log("Down");
        downEvent?.Invoke();
    }

    void OnMouseUp(){
        Debug.Log("Up");
        UpEvent?.Invoke();
    }
}
