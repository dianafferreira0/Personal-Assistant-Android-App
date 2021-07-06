using Didimo.Editor.Utils.Coroutines;
using Didimo.Networking;
using Didimo.Networking.DataObjects;
using Didimo.Utils.Coroutines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DidimoGalleryCollectionFetcher
{
    private float updateFrequencySeconds = 5;
    private string nextPageCursor;
    private string updatePageCursor;
    private CoroutineManager coroutineManager;
    private System.Action<List<DidimoAvatarDataObject>> nextPageDelegate;
    private System.Action<List<DidimoAvatarDataObject>, List<string>> updateDelegate;
    private bool waitingForNextPage = false;
    private bool shouldFetchOneMorePage = false;
    private int numberOfElements = 0;

    // We will need to to keep a record of how many didimos we have. sometimes the API returns repeated deleted didimos, so too keep count we need to keep track of all of them and check if we are really deleting a didimo
    private HashSet<string> didimoCodes;

    public void Stop()
    {
        coroutineManager.StopAllCoroutines();
    }

    public int GetNumberOfElements()
    {
        return numberOfElements;
    }

    public void RemoveDidimoWithKey(string key)
    {
        if (didimoCodes.Remove(key))
        {
            numberOfElements--;
        }
    }

    public bool IsFetchMoreRequestPending()
    {
        return waitingForNextPage;
    }

    public DidimoGalleryCollectionFetcher(CoroutineManager coroutineManager, System.Action<List<DidimoAvatarDataObject>> nextPageDelegate, System.Action<List<DidimoAvatarDataObject>, List<string>> updateDelegate)
    {
        this.coroutineManager = coroutineManager;

        didimoCodes = new HashSet<string>();

        this.nextPageDelegate = nextPageDelegate;
        this.updateDelegate = updateDelegate;

        if (this.coroutineManager == null)
            this.coroutineManager = new EditorCoroutineManager();

       this.coroutineManager.StartCoroutine(ForceInitialization(nextPageDelegate, updateDelegate, 0f));
    }

    private IEnumerator ForceInitialization(System.Action<List<DidimoAvatarDataObject>> nextPageDelegate, System.Action<List<DidimoAvatarDataObject>, List<string>> updateDelegate, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (coroutineManager == null)
            coroutineManager = new EditorCoroutineManager();

        ServicesRequests.EditorInstance.GetDidimoPaginatedList(coroutineManager,
        paginatedDidimoList =>
        {
            if (paginatedDidimoList != null)
            {
                numberOfElements = paginatedDidimoList.totalSize;
                nextPageCursor = paginatedDidimoList.nextCursor;
                updatePageCursor = paginatedDidimoList.updateCursor;

                //Debug.Log("Init - Adding # models: "+ paginatedDidimoList.models.Count);
                foreach (DidimoAvatarDataObject didimo in paginatedDidimoList.models)
                {
                    didimoCodes.Add(didimo.key);
                }

                if (paginatedDidimoList.ttl != 0)
                {
                    updateFrequencySeconds = paginatedDidimoList.ttl * 1;
                }
                nextPageDelegate(paginatedDidimoList.models);
            }
            if (coroutineManager == null)
                coroutineManager = new EditorCoroutineManager();
            coroutineManager.StartCoroutine(Update());
        },
        exception =>
        {
            //Debug.LogError(exception.Message);
            /*if (coroutineManager == null)
                coroutineManager = new EditorCoroutineManager();
            coroutineManager.StartCoroutine(ForceInitialization(nextPageDelegate, updateDelegate, 5f));*/
        });
    }

    public void FetchNextPage()
    {
        // If there is no next page, return
        if (string.IsNullOrEmpty(nextPageCursor))
        {
            return;
        }

        if (waitingForNextPage)
        {
            shouldFetchOneMorePage = true;
            return;
        }
        waitingForNextPage = true;

        if (coroutineManager == null)
            coroutineManager = new EditorCoroutineManager();

        ServicesRequests.EditorInstance.DidimoPaginatedListForCursor(coroutineManager,
            nextPageCursor,
            paginatedDidimoList =>
            {
                numberOfElements = paginatedDidimoList.totalSize;
                nextPageCursor = paginatedDidimoList.nextCursor;

                foreach (DidimoAvatarDataObject didimo in paginatedDidimoList.models)
                {
                    didimoCodes.Add(didimo.key);
                }

                if (paginatedDidimoList.ttl != 0)
                {
                    updateFrequencySeconds = paginatedDidimoList.ttl;
                }

                waitingForNextPage = false;
                nextPageDelegate(paginatedDidimoList.models);

                if (shouldFetchOneMorePage)
                {
                    shouldFetchOneMorePage = false;
                    FetchNextPage();
                }
            },
            exception =>
            {
                Debug.LogError(exception.Message);
                waitingForNextPage = false;
                shouldFetchOneMorePage = false;
            });
    }

    public void ForceUpdate(UnityAction onUpdateDone, UnityAction<System.Exception> onFailure)
    {
        if (coroutineManager == null)
            coroutineManager = new EditorCoroutineManager();
        coroutineManager.StartCoroutine(Update(onUpdateDone, onFailure));

    }

    private IEnumerator Update(UnityAction onUpdateDone = null, UnityAction<System.Exception> onFailure = null)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(updateFrequencySeconds);

            ServicesRequests.EditorInstance.DidimoPaginatedListForCursor(
                new EditorCoroutineManager(),
                updatePageCursor,
                paginatedDidimoList =>
                {
                    foreach (string removed in paginatedDidimoList.removed)
                    {
                        if (didimoCodes.Remove(removed))
                        {
                            numberOfElements--;
                        }
                    }

                    foreach (DidimoAvatarDataObject updated in paginatedDidimoList.updated)
                    {
                        if (didimoCodes.Add(updated.key))
                        {
                            numberOfElements++;
                        }
                    }

                    updatePageCursor = paginatedDidimoList.updateCursor;

                    if (paginatedDidimoList.ttl != 0)
                    {
                        updateFrequencySeconds = paginatedDidimoList.ttl;
                    }

                    updateDelegate(paginatedDidimoList.updated, paginatedDidimoList.removed);

                    if (onUpdateDone != null)
                    {
                        onUpdateDone();
                    }
                },
                exception =>
                {
                    if (onFailure != null)
                    {
                        onFailure(exception);
                    }
                    //Debug.LogError("Error while trying to update gallery: " + exception.Message);
                }
            );
        }
    }
}