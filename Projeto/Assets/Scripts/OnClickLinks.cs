using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnClickLinks : MonoBehaviour
{
    
    public void OnClickHCI()
    {
        Application.OpenURL("https://hcilab.utad.pt/");
    }

    public void OnClickUTAD()
    {
        Application.OpenURL("https://www.utad.pt");
    }

    public void OnClickINESCTEC()
    {
        Application.OpenURL("https://www.inesctec.pt/en");
    }

}
