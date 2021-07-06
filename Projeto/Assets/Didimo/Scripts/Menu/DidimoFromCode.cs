using Didimo.DidimoManagement;
using Didimo.Networking;
using Didimo.Utils.Coroutines;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu
{
    public class DidimoFromCode : DidimoManagerMonoBehaviour
    {
        public static string autoLoadDidimoWithCode;

        public DidimoMenuHandler mainMenu;

        private void Start()
        {
            if (!string.IsNullOrEmpty(autoLoadDidimoWithCode))
            {
                GetDidimoFromCode(autoLoadDidimoWithCode);
                autoLoadDidimoWithCode = null;
            }
        }

        public void GetDidimoFromCode(Text code)
        {
            GetDidimoFromCode(code.text);
        }

        public void GetDidimoFromCode(string didimoKey)
        {
            GameCoroutineManager coroutineManager = new GameCoroutineManager();

            LoadingOverlay.Instance.ShowLoadingMenu(() =>
            {
                coroutineManager.StopAllCoroutines();
            });

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
        }
    }
}
