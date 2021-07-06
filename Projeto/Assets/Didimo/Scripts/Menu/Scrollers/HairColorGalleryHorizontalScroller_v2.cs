using Didimo.Networking.DataObjects;
using Didimo.Utils;
using Didimo.Utils.Coroutines;
using Mopsicus.InfiniteScroll;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Didimo.Menu.Scrollers
{
    public class HairColorGalleryHorizontalScroller_v2 : MonoBehaviour
    {
        [SerializeField]
        private InfiniteScroll Scroll = null;

        [SerializeField]
        private int itemWidth;
        public GameObject templateItem;
        public int itemSpacing = 0;

        private List<int> _widths = new List<int>();
        protected List<ItemDataObject> _list_data;

        public Action<Toggle, int, MaterialState> onItemToggleEventDelegate = null;

        [SerializeField]
        protected TextAsset[] hairstyle1_material_states;


        private CoroutineManager coroutineManager;

        private void Init()
        {
            coroutineManager = new GameCoroutineManager();
            Scroll.OnFill += OnFillItem;
            Scroll.OnWidth += OnWidthItem;
        }

        public Color GetColorAt(int index)
        {
            return _list_data[index].color;
        }

        public MaterialState GetMaterialStateAt(int index)
        {
            if (_list_data == null)
                return GetMaterialStates()[index];//InitializeHairColorGalleryScroller();
            else return _list_data[index].materialState;
        }

        int OnWidthItem(int index)
        {
            return _widths[index];
        }

        List<MaterialState> GetMaterialStates()
        {
            List<MaterialState> result = new List<MaterialState>();
            foreach (TextAsset materialStateJson in hairstyle1_material_states)
            {
                MaterialState materialState = Didimo.Networking.DataObject.LoadFromJson<MaterialState>(materialStateJson.text);

                Color color = new Color();
                foreach (MaterialState.Material.Parameter parameter in materialState.materials[0].parameters)
                {
                    if (parameter.name == "diffColor")
                    {
                        color = parameter.GetColor();
                        break;
                    }
                }

                result.Add(materialState);
            }

            return result;
        }

        Color GetDiffuseColorFromMaterial(MaterialState.Material material)
        {
            Color color = new Color();
            foreach (MaterialState.Material.Parameter parameter in material.parameters)
            {
                if (parameter.name == "diffColor")
                {
                    color = parameter.GetColor();
                    break;
                }
            }
            return color;
        }

        public void InitializeHairColorGalleryScroller()
        {
            if (coroutineManager == null)
                Init();

            Scroll.RecycleAll();
            _list_data = new List<ItemDataObject>();
            _widths = new List<int>();

            itemWidth = (int)templateItem.transform.parent.GetComponent<RectTransform>().rect.height;

            templateItem.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (!templateItem.activeInHierarchy)
            {
                Debug.LogError("Getting the rect transform of an inactive component will yield unpredictable results.");
            }
            itemWidth = (int)templateItem.transform.parent.GetComponent<RectTransform>().rect.height;
            templateItem.SetActive(false);
            List<MaterialState> materialStates = GetMaterialStates();

            for (int i = 0; i < materialStates.Count; i++)
            {

                Color color = GetDiffuseColorFromMaterial(materialStates[i].materials[0]);
                ItemDataObject it = new ItemDataObject(i.ToString(), color, materialStates[i]);
                _list_data.Add(it);
                _widths.Add(itemWidth);
            }

            Scroll.InitData(_list_data.Count);
        }

        public List<Color> GenerateColorList(ref List<string> hairColorNames)
        {
            List<Color> list = new List<Color>();

            // TODO: Improve this. we are probably serializing the material states twice (see InitializeHairColorGalleryScroller)
            if (_list_data != null)
            {
                for (int i = 0; i < hairstyle1_material_states.Length; i++) //fade between black and dark brown
                {
                    Color color = _list_data[i].color;
                    list.Add(color);
                    if (hairColorNames != null)
                    {
                        hairColorNames.Add(_list_data[i].key);
                    }
                }
            }

            else
            {
                List<MaterialState> materialStates = GetMaterialStates();
                for (int i = 0; i < materialStates.Count; i++)
                {
                    Color color = GetDiffuseColorFromMaterial(materialStates[i].materials[0]);
                    list.Add(color);
                    if (hairColorNames != null)
                    {
                        hairColorNames.Add(materialStates[i].materials[0].name);
                    }
                }
            }

            return list;
        }

        void OnFillItem(int index, GameObject item)
        {
            if (index < 0 || index > _list_data.Count - 1)
                return;
            Transform t = item.transform;

            t.FindRecursive("Mask").GetComponent<Image>().color = new Color(_list_data[index].color.r, _list_data[index].color.g, _list_data[index].color.b);

            t.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            t.GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool active)
            {
                if (active)
                {
                    onItemToggleEventDelegate(t.GetComponent<Toggle>(), index, _list_data[index].materialState);
                }
            });
        }


        protected class ItemDataObject
        {
            public ItemDataObject(string key, Color value, MaterialState matState)
            {
                this.key = key;
                this.color = value;
                materialState = matState;
            }

            public string key;
            public Color color;
            public MaterialState materialState;
        }
    }
}
