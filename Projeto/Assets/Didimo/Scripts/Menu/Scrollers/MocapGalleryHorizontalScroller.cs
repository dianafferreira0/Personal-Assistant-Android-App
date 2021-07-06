using System.Collections.Generic;
using System.IO;
using Didimo.Utils.Coroutines;
using Mopsicus.InfiniteScroll;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Didimo.Utils;
using System;

namespace Didimo.Menu.Scrollers
{
    public class MocapGalleryHorizontalScroller : MonoBehaviour
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
        protected Sprite[] cropped_images;

        public List<string> defaultMocapAnimations;

        int newRecsCount = 0;

        private List<Action<Toggle, int>> onItemToggleEventDelegates = new List<Action<Toggle, int>>();

        public void RegisterOnItemToggleEventDelegate(Action<Toggle, int> action)
        {
            onItemToggleEventDelegates.Add(action);
        }

        public void UnOnItemToggleEventDelegate(Action<Toggle, int> action)
        {
            onItemToggleEventDelegates.Remove(action);
        }

        private CoroutineManager coroutineManager;

        private void Init()
        {
            coroutineManager = new GameCoroutineManager();
            cropped_images = null;
            Scroll.OnFill += OnFillItem;
            Scroll.OnWidth += OnWidthItem;
        }

        void OnFillItem(int index, GameObject item)
        {
            if (index < 0 || index > _list_data.Count - 1)
                return;

            Transform t = item.transform;
            t.FindRecursive("ItemImage").GetComponent<Image>().sprite = cropped_images[index];
            t.FindRecursive("NameLabel").GetComponent<Text>().text = _list_data[index].name;

            t.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
            t.GetComponent<Toggle>().onValueChanged.AddListener(delegate
            {
                foreach (Action<Toggle, int> action in onItemToggleEventDelegates)
                {
                    action(t.GetComponent<Toggle>(), index);
                }
            });
        }

        int OnWidthItem(int index)
        {
            return _widths[index];
        }

        public string GetItemName(int index)
        {
            return _list_data[index].name;
        }

        //returns negative value if the item is not a default mocap animation
        public int GetDefaultItemIndex(int index)
        {
            return index - newRecsCount;
        }


        public void InitializeMocapRecordingsGalleryScroller(string[] fileNames, string[] defaultMocapAnimations)
        {
            if (coroutineManager == null)
                Init();
            this.defaultMocapAnimations = new List<string>(defaultMocapAnimations);

            Scroll.RecycleAll();
            //Debug.LogWarning("MocapRecordings - InitializeGallery");
            _list = new List<int>();
            _list_data = new List<ItemDataObject>();
            _widths = new List<int>();

            itemWidth = (int)templateItem.transform.GetComponent<RectTransform>().rect.height;
        templateItem.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (!templateItem.activeInHierarchy)
        {
            Debug.LogError("Getting the rect transform of an inactive component will yield unpredictable results.");
        }
        itemWidth = (int)templateItem.transform.GetComponent<RectTransform>().rect.height;
        templateItem.SetActive(false);

            Sprite defaultSprite = templateItem.transform.FindRecursive("ItemImage").GetComponent<Image>().sprite;

            cropped_images = new Sprite[fileNames.Length + defaultMocapAnimations.Length]; //also add room for default mocap animations

            for (int i = 0; i < fileNames.Length; i++)
            {
                string[] parsedNames = fileNames[i].Split('_');
                string parsedName = "";
                for (int j = 1; j < parsedNames.Length; j++)
                {
                    parsedName += parsedNames[j] + " ";
                }
                parsedName = parsedName.Replace(".txt", "");
                ItemDataObject it = new ItemDataObject(i.ToString(), parsedName.TrimEnd(), defaultSprite);
                _list_data.Add(it);
                _widths.Add(itemWidth);
                _list.Add(i);
                cropped_images[i] = defaultSprite;
            }

            for (int i = fileNames.Length; i < cropped_images.Length; i++)
            {
                string defaultAnimName = "Anim " + (i - fileNames.Length + 1);

                string[] nameParts = defaultMocapAnimations[(i - fileNames.Length + 0)].Split("/".ToCharArray());
                defaultAnimName = nameParts[nameParts.Length - 1];

                if (defaultAnimName.Equals("simpleROM"))
                    defaultAnimName = "Simple";
                else if (defaultAnimName.Equals("faceROM"))
                    defaultAnimName = "Advanced";


                //add the default mocap animation at the end of the list
                ItemDataObject defaultMocapIt = new ItemDataObject(i.ToString(), defaultAnimName, defaultSprite);
                _list_data.Add(defaultMocapIt);
                _widths.Add(itemWidth);
                _list.Add(fileNames.Length);
                cropped_images[i] = defaultSprite;
            }

            Debug.Log("MocapRecordings - InitializeGallery - init data " + _list.Count + " itemWidth=" + itemWidth);
            Scroll.InitData(_list.Count, templateItem.transform.GetComponent<RectTransform>());

            templateItem.SetActive(false);

            newRecsCount = fileNames.Length;
        }

        public void RenameMocapRecordingAtIndex(string newName, int index)
        {
            Debug.Log("MocapRecordings - RenameMocapRecordingAtIndex");
            if (index < 0 || index > _list_data.Count - 1)
                return;

            _list_data[index].name = newName;
            Scroll.UpdateVisible();
        }

        protected class ItemDataObject
        {
            public ItemDataObject(string key, string name, Sprite sprite)
            {
                this.key = key;
                this.name = name;
                this.sprite = sprite;
            }

            public Sprite sprite;
            public string key;
            public string name;
        }

    }
}
