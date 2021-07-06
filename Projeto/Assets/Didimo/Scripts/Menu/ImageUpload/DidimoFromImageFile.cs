using Didimo.DidimoManagement;
using Didimo.Menu.FileBrowser;
using Didimo.Networking;
using Didimo.Utils.Coroutines;
using UnityEngine;

namespace Didimo.Menu.ImageUpload
{

    /// <summary>
    /// This MonoBehaviour allows us to select a photo file, and to create a Didimo from it.
    /// </summary>
    public class DidimoFromImageFile : DidimoManagerMonoBehaviour
    {
        [SerializeField]
        ImageFileBrowser fileBrowser = null;
        CoroutineManager coroutineManager;

        public DidimoMenuHandler mainMenu;

        // Use this for initialization
        void Start()
        {
            coroutineManager = new GameCoroutineManager();
            fileBrowser.OnOpenAction = path =>
            {

                ServicesRequests.GameInstance.CreateDidimoCheckProgress(
                    coroutineManager,
                    Didimo.Networking.InputType.Photo,
                    path,
                    didimoKey =>
                    {
                        LoadingOverlay.Instance.ShowLoadingMenu(() =>
                        {
                            coroutineManager.StopAllCoroutines();
                        }, "Downloading...");

                        ServicesRequests.GameInstance.GetDidimoDefinitionModel(
                        coroutineManager,
                        didimoKey,
                        (didimoDefinitionModel) =>
                        {
                            Debug.Log("GetDidimoDefinitionModel complete! Importing...");
                            StartCoroutine(mainMenu.DidimoImporter.ImportDidimo(coroutineManager, didimoKey, didimoDefinitionModel.meta,
                                didimoGameObject =>
                                {
                                    Debug.Log("Didimo loading is complete!");
                                    LoadingOverlay.Instance.Hide();
                                    if (mainMenu != null)
                                    {
                                        mainMenu.HideMenu();
                                        mainMenu.InitPlayerPanel(didimoGameObject, didimoKey, didimoDefinitionModel.meta);
                                    }
                                },
                                exception =>
                                {
                                    mainMenu.ClearAnimButtonsPanel();
                                    ErrorOverlay.Instance.ShowError(exception.Message);
                                }
                            ));
                        },
                        exception =>
                        {
                            mainMenu.ClearAnimButtonsPanel();
                            ErrorOverlay.Instance.ShowError(exception.Message);
                        }
                    );
                    },
                    exception =>
                    {
                        ErrorOverlay.Instance.ShowError(exception.Message);
                    }, progress =>
                    {
                        LoadingOverlay.Instance.ShowProgress(progress);
                    });

                LoadingOverlay.Instance.ShowLoadingMenu(() =>
                {
                    coroutineManager.StopAllCoroutines();
                }, "Processing...");
            };
        }
    }
}
