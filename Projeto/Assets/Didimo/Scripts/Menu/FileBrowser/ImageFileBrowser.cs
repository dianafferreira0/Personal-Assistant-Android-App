using Didimo.Menu.Utils;
using Didimo.Utils.Coroutines;
using Didimo.Utils.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Didimo.Menu.FileBrowser
{

    /// <summary>
    /// A file browser, that will allow the selection of an image from the hard drive.
    /// Has image preview, folder navigation, and favorites management.
    /// </summary>
    public class ImageFileBrowser : MonoBehaviour
    {
        const int fileAndDirsPoolSize = 20;

        public System.Action<string> OnOpenAction;
        public System.Action OnCancelAction;

        public string directoryCharacter = "m";
        public string fileCharacter = "h";
        [SerializeField]
        Dropdown fileTypeDropDown = null;
        [SerializeField]
        Button addFavoriteButton = null;
        [SerializeField]
        Button deleteButton = null;
        [SerializeField]
        Button goBackButton = null;
        [SerializeField]
        Button goForwardButton = null;
        [SerializeField]
        Button goUpButton = null;
        [SerializeField]
        IconImageTextLayout fileTemplate = null;
        [SerializeField]
        IconTextLayout favoriteTemplate = null;
        [SerializeField]
        IconTextLayout driveTemplate = null;
        [SerializeField]
        InputField pathInputField = null;
        [SerializeField]
        GameObject errorMenu = null;
        [SerializeField]
        Text errorMenuText = null;

        List<IconImageTextLayout> filesAndDirs;
        int selectedFavoriteIndex;
        RectTransform filesParent;
        RectTransform favoritesAndDrivesParent;
        GameObject objectPool;
        GameObject templates;
        List<TextureUpdate> textureUpdates;
        CoroutineManager coroutineManager;
        List<IconTextLayout> drivesLayout;
        List<IconTextLayout> favoritesLayout;
        List<DirectoryInfo> directoryHistory;
        int currentDirIndex = 0;
        DirectoryInfo currentDirectory;
        string[] drives;
        List<string> fileExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
        List<string> favorites;
        int currentFileLayout;
        string selectedFolderPath;
        string selectedFilePath;

        IconImageTextLayout GetNextFileLayout()
        {
            currentFileLayout++;

            if (currentFileLayout >= filesAndDirs.Count)
            {
                IconImageTextLayout i = Instantiate(fileTemplate);
                filesAndDirs.Add(i);
            }

            return filesAndDirs[currentFileLayout];
        }


        private void Start()
        {
            errorMenu.SetActive(false);
            pathInputField.onEndEdit.AddListener(text =>
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Refresh();
                }
            });
            textureUpdates = new List<TextureUpdate>();
            coroutineManager = new GameCoroutineManager();
            addFavoriteButton.interactable = false;
            deleteButton.interactable = false;
            currentDirIndex = -1;
            objectPool = new GameObject("ObjectPool");
            objectPool.SetActive(false);
            objectPool.transform.parent = transform;

            templates = new GameObject("Templates");
            templates.SetActive(false);
            templates.transform.parent = transform;

            fileTypeDropDown.options.Clear();
            fileTypeDropDown.options.Add(new Dropdown.OptionData() { text = "Image files (*.jpg, *.jpeg, *.gif)" });
            fileTypeDropDown.RefreshShownValue();
            filesAndDirs = new List<IconImageTextLayout>();

            currentFileLayout = -1;
            while (currentFileLayout < fileAndDirsPoolSize)
            {
                GetNextFileLayout();
            }

            ResetLayout();
            directoryHistory = new List<DirectoryInfo>();
            filesParent = fileTemplate.transform.parent as RectTransform;
            fileTemplate.transform.SetParent(templates.transform);

            favoriteTemplate.transform.SetParent(objectPool.transform);

            favoritesAndDrivesParent = driveTemplate.transform.parent as RectTransform;
            driveTemplate.transform.SetParent(templates.transform);

            drives = Directory.GetLogicalDrives();

            drivesLayout = new List<IconTextLayout>();
            foreach (string drive in drives)
            {
                IconTextLayout it = Instantiate(driveTemplate);
                drivesLayout.Add(it);
                it.transform.SetParent(favoritesAndDrivesParent);
                it.Configure(fileCharacter[0], drive);
                DoubleClickableButton b = it.GetComponent<DoubleClickableButton>();

                UnityAction action = delegate
                {
                    selectedFolderPath = null;
                    SetCurrentDir(drive);
                    //Deselect the button
                    EventSystem.current.SetSelectedGameObject(null);
                };

                b.onDoubleClickAction =
                    () =>
                {
                    action.Invoke();
                    selectedFilePath = null;
                };

                b.onClick.AddListener(() =>
                {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        action.Invoke();
                    }
                    selectedFilePath = null;
                });
            }

            SetCurrentDir(drives[0]);
            LoadFavorites();
        }

        private void OnDestroy()
        {
            Destroy(objectPool);
            Destroy(templates);
        }

        public void Refresh()
        {
            if (File.Exists(pathInputField.text))
            {
                selectedFilePath = pathInputField.text;
                Open();
            }
            else
            {
                SetCurrentDir(pathInputField.text);
            }
        }

        public void ClickedFavoriteButton()
        {
            if (selectedFolderPath != null)
            {
                favorites.Add(selectedFolderPath);
                SaveFavorites();
                UpdateFavoritesUI();
            }
        }

        public void ClickedDeleteButton()
        {
            if (selectedFavoriteIndex >= 0 && selectedFavoriteIndex < favorites.Count)
            {
                favorites.RemoveAt(selectedFavoriteIndex);
                SaveFavorites();
                UpdateFavoritesUI();
            }
            deleteButton.interactable = false;
        }

        string favoritesKey = "Didimo.FileBrowser.Favorites";
        void LoadFavorites()
        {
            string favoritesJson = PlayerPrefs.GetString(favoritesKey);
            if (favoritesJson != null)
            {
                favorites = MiniJSON.Deserialize<List<string>>(favoritesJson);
            }

            if (favorites == null)
            {
                favorites = new List<string>();
            }

            UpdateFavoritesUI();
        }

        void SaveFavorites()
        {
            PlayerPrefs.SetString(favoritesKey, MiniJSON.Serialize(favorites));
        }

        void UpdateFavoritesUI()
        {
            if (favoritesLayout == null)
            {
                favoritesLayout = new List<IconTextLayout>();
            }

            foreach (IconTextLayout favoriteLayout in favoritesLayout)
            {
                DestroyImmediate(favoriteLayout.gameObject);
            }
            favoritesLayout.Clear();

            if (favorites.Count == 0)
            {
                IconTextLayout favoriteLayout = Instantiate(favoriteTemplate, favoritesAndDrivesParent.transform);
                favoritesLayout.Add(favoriteLayout);
                favoriteLayout.transform.SetSiblingIndex(1);
                favoriteLayout.GetComponent<Button>().interactable = false;
                favoriteLayout.Configure('p', "You have no favorites");
            }
            else
            {
                for (int index = 0; index < favorites.Count; index++)
                {
                    string favorite = favorites[index];

                    if (!AddFavoriteUI(favorite, index))
                    {
                        favorites.RemoveAt(index);
                        index--;
                    }
                }
            }
        }

        bool AddFavoriteUI(string favorite, int index)
        {
            if (!Directory.Exists(favorite))
            {
                return false;
            }

            IconTextLayout favoriteLayout = Instantiate(favoriteTemplate, favoritesAndDrivesParent.transform);
            favoriteLayout.transform.SetSiblingIndex(index + 1);
            favoritesLayout.Add(favoriteLayout);
            DoubleClickableButton b = favoriteLayout.GetComponent<DoubleClickableButton>();

            UnityAction action = delegate
            {
                SetCurrentDir(favorite);
                selectedFavoriteIndex = -1;
                deleteButton.interactable = false;
            };
            b.onClick.AddListener(() =>
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    action.Invoke();
                }
                else
                {
                    selectedFilePath = null;
                    selectedFavoriteIndex = favoriteLayout.transform.GetSiblingIndex() - 1;
                    deleteButton.interactable = true;
                }
            });
            b.onDoubleClickAction = () =>
            {
                action.Invoke();
            };

            favoriteLayout.Configure('\0', Path.GetFileName(favorite));
            return true;
        }

        void ResetLayout()
        {
            currentFileLayout = -1;
            foreach (IconImageTextLayout i in filesAndDirs)
            {
                i.transform.SetParent(objectPool.transform);
            }
        }

        private void SetCurrentDir(string dirPath, bool addToHistory = true)
        {
            selectedFilePath = null;
            if (!Directory.Exists(dirPath))
            {
                ShowInvalidPathError();
                return;
            }

            coroutineManager.StopAllCoroutines();
            textureUpdates.Clear();

            if (currentDirectory != null && currentDirectory.FullName.Equals(dirPath))
            {
                addToHistory = false;
            }

            ResetLayout();
            currentDirectory = new DirectoryInfo(dirPath);
            goUpButton.interactable = currentDirectory.Parent != null;

            if (addToHistory)
            {
                if (currentDirIndex < directoryHistory.Count - 1)
                {
                    directoryHistory.RemoveRange(currentDirIndex + 1, directoryHistory.Count - (currentDirIndex + 1));
                }
                directoryHistory.Add(currentDirectory);
                currentDirIndex++;
            }

            goBackButton.interactable = currentDirIndex > 0;
            goForwardButton.interactable = currentDirIndex < (directoryHistory.Count - 1);

            pathInputField.text = currentDirectory.FullName;

            DirectoryInfo[] directories = currentDirectory.GetDirectories();
            FileInfo[] files = currentDirectory.GetFiles();

            foreach (DirectoryInfo dir in directories)
            {
                if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }

                IconImageTextLayout iit = GetNextFileLayout();
                iit.transform.SetParent(filesParent);
                iit.Configure(directoryCharacter[0], dir.Name);
                DoubleClickableButton b = iit.GetComponent<DoubleClickableButton>();

                b.onClick.AddListener(() =>
               {
                   if (Input.GetKeyDown(KeyCode.Return))
                   {
                       SetCurrentDir(dir.FullName);
                   }
                   else
                   {
                       selectedFilePath = null;
                       selectedFolderPath = dir.FullName;
                       addFavoriteButton.interactable = !favorites.Contains(selectedFolderPath);
                   }
               });

                b.onDoubleClickAction =
                    () =>
                        {
                            SetCurrentDir(dir.FullName);
                        };
            }

            foreach (FileInfo file in files)
            {
                if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                    !fileExtensions.Contains(file.Extension))
                {
                    continue;
                }

                IconImageTextLayout iit = GetNextFileLayout();
                iit.transform.SetParent(filesParent);
                iit.Configure(fileCharacter[0], file.Name);
                DoubleClickableButton b = iit.GetComponent<DoubleClickableButton>();

                b.onClick.AddListener(() =>
                {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        Open();
                    }
                    else
                    {
                        selectedFolderPath = null;
                        selectedFilePath = file.FullName;
                    }
                });

                b.onDoubleClickAction =
                    () =>
                    {
                        Open();
                    };
                textureUpdates.Add(new TextureUpdate(iit, file.FullName));
            }

            if (textureUpdates.Count > 0)
            {
                coroutineManager.StartCoroutine(DownloadTextures());
            }
        }

        public void GoBack()
        {
            if (currentDirIndex > 0)
            {
                currentDirIndex--;
                SetCurrentDir(directoryHistory[currentDirIndex].FullName, false);
            }
        }

        public void GoForward()
        {
            if (currentDirIndex < directoryHistory.Count - 1)
            {
                currentDirIndex++;
                SetCurrentDir(directoryHistory[currentDirIndex].FullName, false);
            }
        }

        public void GoUp()
        {
            SetCurrentDir(currentDirectory.Parent.FullName);
        }

        void ShowInvalidPathError()
        {
            errorMenuText.text = "Invalid Path";
            errorMenu.SetActive(true);
        }

        public void Open()
        {
            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                SetCurrentDir(selectedFolderPath);
            }
            else if (!string.IsNullOrEmpty(selectedFilePath))
            {
                if (File.Exists(selectedFilePath))
                {
                    OnOpenAction(selectedFilePath);
                }
                else
                {
                    ShowInvalidPathError();
                }
            }
        }

        public void Cancel()
        {
            if (OnCancelAction != null)
            {
                OnCancelAction();
            }
        }

        private IEnumerator DownloadTextures()
        {
            while (textureUpdates.Count > 0)
            {
                TextureUpdate t = textureUpdates[0];
                UnityWebRequest textureWWW = UnityWebRequestTexture.GetTexture("file:///" + t.texturePath);

                yield return textureWWW.SendWebRequest();

                Texture texture = ((DownloadHandlerTexture)textureWWW.downloadHandler).texture;
                t.fileLayout.Configure(texture, Path.GetFileName(t.texturePath));

                textureUpdates.RemoveAt(0);
            }
        }

        private class TextureUpdate
        {
            public IconImageTextLayout fileLayout;
            public string texturePath;

            public TextureUpdate(IconImageTextLayout fileLayout, string texturePath)
            {
                this.fileLayout = fileLayout;
                this.texturePath = texturePath;
            }
        }
    }
}