using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
public class AdsManager : MonoBehaviour
{
    #if UNITY_ANDROID
    private string gameId = "4188088";
    #endif

    bool testMode = false;

    void Start()
    {
        Advertisement.Initialize(gameId, testMode);
    }

    public void ShowInterstitialAd()
    {
        if(Advertisement.IsReady())
        Advertisement.Show();
        else
        Debug.Log("Ad not ready!");
    }
}
