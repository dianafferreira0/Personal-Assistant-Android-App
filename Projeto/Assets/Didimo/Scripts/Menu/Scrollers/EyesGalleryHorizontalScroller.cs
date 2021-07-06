using System.Collections.Generic;
using System.IO;
using Didimo.Menu;
using Didimo.Utils;
using Didimo.Utils.Coroutines;
using Mopsicus.InfiniteScroll;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Didimo.Menu.Scrollers
{
    public class EyesGalleryHorizontalScroller : MonoBehaviour
    {

        [SerializeField]
        private InfiniteScroll Scroll = null;

        [SerializeField]
        private int itemWidth;
        public GameObject templateItem;
        public int itemSpacing = 0;

        private List<int> _list = new List<int>();
        private List<int> _widths = new List<int>();
        protected List<ItemDataObject> _list_data;
        protected Sprite[] buttonImages;
        public Texture2D[] textureImages;

        public GameObject didimoParentPlaceholder;
        public Texture2D currentlyActiveTexture;

        private CoroutineManager coroutineManager;

        protected Texture defaultPipelineTexture;
        //protected int current_eyeTexture_index;
        protected int temporary_eyeTexture_index;
        public int EyeTextureIndexFromUserPrefs
        {
            get
            {
                string didimoCode = PlayerPrefs.GetString("CurrentDidimoCode", null);
                string eyeTexturePrefsKey = "EditDidimo_EyeTextureIndex_" + didimoCode;
                return PlayerPrefs.HasKey(eyeTexturePrefsKey) ? PlayerPrefs.GetInt(eyeTexturePrefsKey) : 0;
            }

            set
            {
                string didimoCode = PlayerPrefs.GetString("CurrentDidimoCode", null);
                string eyeTexturePrefsKey = "EditDidimo_EyeTextureIndex_" + didimoCode;
                PlayerPrefs.SetInt(eyeTexturePrefsKey, value);
            }
        }

        public bool HasEyeTextureIndexBeenSetForThisDidimo()
        {
            string didimoCode = PlayerPrefs.GetString("CurrentDidimoCode", null);
            string eyeTexturePrefsKey = "EditDidimo_EyeTextureIndex_" + didimoCode;
            return PlayerPrefs.HasKey(eyeTexturePrefsKey);
        }

        public void Start()
        {
        }

        private void Init()
        {
            if (coroutineManager == null)
            {
                coroutineManager = new GameCoroutineManager();
                Scroll.OnFill += OnFillItem;
                Scroll.OnWidth += OnWidthItem;
            }
            RefreshAndLoadAvailableEyeVariationsFromComponent();
        }

        void OnFillItem(int index, GameObject item)
        {
            if (index < 0 || index > _list_data.Count - 1)
                return;
            else
            {
                Transform t = item.transform;
                t.FindRecursive("ItemImage").GetComponent<Image>().sprite = buttonImages[index];
                t.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
                t.GetComponent<Toggle>().onValueChanged.AddListener(delegate
                {
                    temporary_eyeTexture_index = index;
                    ChangeEyesToIndex(index, textureImages[index]);
                });
            }
        }

        public void ChangeEyesToIndex(int index, Texture2D texture)
        {
            currentlyActiveTexture = texture;
            Transform parentGeometry = didimoParentPlaceholder.transform.FindRecursive("RootNode");
            if (parentGeometry == null)
                parentGeometry = didimoParentPlaceholder.transform.FindRecursive("Geometry");
            if (parentGeometry != null)
            {
                MeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mesh in meshes)
                {
                    if (mesh.name.ToLower().Contains("cornea"))
                    {
                        if (!HasEyeTextureIndexBeenSetForThisDidimo()) //save the value in order to be able to reset
                            defaultPipelineTexture = mesh.material.GetTexture("colorSampler");
                        mesh.material.SetTexture("colorSampler", texture);
                    }
                }
            }
            else Debug.Log("eyes manager - RootNode not found");
        }
        public void ChangeEyesToTexture(Texture texture)
        {
            currentlyActiveTexture = null;
            Transform parentGeometry = didimoParentPlaceholder.transform.FindRecursive("RootNode");
            MeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mesh in meshes)
            {
                if (mesh.name.ToLower().Contains("cornea"))
                {
                    mesh.material.SetTexture("colorSampler", texture);
                }
            }
        }

        public void RestoreValues()
        {
            if (coroutineManager == null)
                InitializeEyesGalleryScroller();

            if (HasEyeTextureIndexBeenSetForThisDidimo())
            {
                int current_eyeTexture_index = EyeTextureIndexFromUserPrefs;
                Debug.Log("eyes manager - RestoreValues - index: " + current_eyeTexture_index);
                temporary_eyeTexture_index = current_eyeTexture_index;
                ChangeEyesToIndex(current_eyeTexture_index, textureImages[current_eyeTexture_index]);
            }
            else
            {
                Debug.Log("eyes manager - RestoreValues - index not set - using original and saving default texture - START: " + defaultPipelineTexture);
                Transform parentGeometry = didimoParentPlaceholder.transform.FindRecursive("RootNode");
                if (parentGeometry == null)
                    parentGeometry = didimoParentPlaceholder.transform.FindRecursive("Geometry");
                if (parentGeometry != null)
                {
                    MeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mesh in meshes)
                    {
                        if (mesh.name.ToLower().Contains("cornea"))
                        {
                            //if (!HasEyeTextureIndexBeenSetForThisDidimo()) //save the value in order to be able to reset
                            //    defaultPipelineTexture = mesh.material.GetTexture("colorSampler");
                            mesh.material.SetTexture("colorSampler", defaultPipelineTexture);
                        }
                    }
                    Debug.Log("eyes manager - RestoreValues - index not set - using original and saving default texture - END: " + defaultPipelineTexture);
                }
                else Debug.Log("eyes manager - RootNode not found");
            }
        }

        public void SaveDefaultEyeTexture()
        {

            if (!HasEyeTextureIndexBeenSetForThisDidimo())
            {
                Transform parentGeometry = didimoParentPlaceholder.transform.FindRecursive("RootNode");
                if (parentGeometry == null)
                    parentGeometry = didimoParentPlaceholder.transform.FindRecursive("Geometry");
                if (parentGeometry != null)
                {
                    MeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mesh in meshes)
                    {
                        if (mesh.name.ToLower().Contains("cornea"))
                        {
                            //if (!HasEyeTextureIndexBeenSetForThisDidimo()) //save the value in order to be able to reset
                            defaultPipelineTexture = mesh.material.GetTexture("colorSampler");
                        }
                    }
                    Debug.Log("eyes manager - SaveDefaultEyeTexture - using original and saving default texture - END: " + defaultPipelineTexture);
                }
                else Debug.Log("eyes manager - RootNode not found");
            }
        }

        public void FactoryReset()
        {
            string didimoCode = PlayerPrefs.GetString("CurrentDidimoCode", null);
            string eyeTexturePrefsKey = "EditDidimo_EyeTextureIndex_" + didimoCode;
            PlayerPrefs.DeleteKey(eyeTexturePrefsKey);
        }
        public void ResetEdit()
        {
            if (EyeTextureIndexFromUserPrefs != temporary_eyeTexture_index)
            {
                RestoreValues();
            }
        }

        public void SaveEdit()
        {
            if (EyeTextureIndexFromUserPrefs != temporary_eyeTexture_index)
                EyeTextureIndexFromUserPrefs = temporary_eyeTexture_index;
        }

        public string GetCurrentEyeName()
        {
            return _list_data[temporary_eyeTexture_index].key.Replace(" ", string.Empty);
        }
        int OnWidthItem(int index)
        {
            return _widths[index];
        }

        //bool hasInited = false;
        int hasInited = 0;
        public void InitializeEyesGalleryScroller()
        {
            if (hasInited > 1)
                return;
            hasInited++;

            if (coroutineManager == null)
                Init();

            _list = new List<int>();
            _list_data = new List<ItemDataObject>();
            _widths = new List<int>();

            templateItem.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (!templateItem.activeInHierarchy)
            {
                Debug.LogError("Getting the rect transform of an inactive component will yield unpredictable results.");
            }
            templateItem.SetActive(false);
            itemWidth = (int)templateItem.transform.GetComponent<RectTransform>().rect.height;

            for (int i = 0; i < buttonImages.Length; i++)
            {
                ItemDataObject it = new ItemDataObject(i.ToString(), "", buttonImages[i]);
                _list_data.Add(it);
                _widths.Add(itemWidth);
                _list.Add(i);
            }

            Scroll.InitData(_list.Count, templateItem.transform.GetComponent<RectTransform>());
            templateItem.SetActive(false);
        }

        public void RefreshAndLoadAvailableEyeVariationsFromComponent()
        {
            LoadingOverlay.Instance.ShowLoadingMenu(() =>
            {
            }, "Loading...");

            try
            {
                buttonImages = new Sprite[textureImages.Length];
                for (int i = 0; i < textureImages.Length; i++)
                {
                    Texture2D _tex = textureImages[i] as Texture2D;
                    //buttonImages[i] = CropImage(_tex);
                    buttonImages[i] = Sprite.Create(_tex, new Rect(0, 0, _tex.width, _tex.height), Vector2.zero);
                }

                Debug.Log("RefreshAndLoadAvailableEyeVariationsFromComponent (" + textureImages.Length + ")");
                LoadingOverlay.Instance.Hide();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message + " - " + e.StackTrace);
                LoadingOverlay.Instance.Hide();
                ErrorOverlay.Instance.ShowError(e.Message, () => { });
            }
        }

        Sprite CropImage(Texture2D tex)
        {
            //Debug.Log("cropping");
            int crop_x = 115;
            int crop_y = 115;
            //Create second texture to copy the first texture into
            Texture2D CroppedTexture = new Texture2D(tex.width - crop_x, tex.height - crop_y, TextureFormat.RGBA32, false);

            for (int y = 0; y < CroppedTexture.height; y++)
            {
                //Width of image in pixels
                for (int x = 0; x < CroppedTexture.width; x++)
                {
                    Color cPixelColour = tex.GetPixel(x + crop_x / 2, y + crop_y);
                    CroppedTexture.SetPixel(x, y, cPixelColour);
                }
            }

            CroppedTexture.Apply();
            Sprite s = Sprite.Create(CroppedTexture, new Rect(0, 0, CroppedTexture.width, CroppedTexture.height), Vector2.zero);
            //Object.Destroy(CroppedTexture);
            return s;
        }
        public Sprite ConvertToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }


        protected class ItemDataObject
        {
            public ItemDataObject(string key, string name, Sprite sprite)
            {
                this.key = key;
                this.name = name;
                this.sprite = sprite;
            }
            public ItemDataObject(string key, string name, Texture2D texture)
            {
                this.key = key;
                this.name = name;
                this.texture = texture;
            }

            public Texture2D texture;
            public Sprite sprite;
            public string key;
            public string name;
        }

    }
}
