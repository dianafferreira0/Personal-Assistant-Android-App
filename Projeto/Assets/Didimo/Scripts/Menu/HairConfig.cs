using Didimo.Networking;
using Didimo.Utils.Coroutines;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using Didimo.Menu;
using Didimo.Utils;
using Didimo.Networking.DataObjects;


using System.Collections;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using Didimo.Menu.Scrollers;

public partial class HairConfig : HairConfigBase
{

}

public class HairConfigBase : MonoBehaviour
{
    // Inspector variables
    //[SerializeField]
    //public GameObject hair_placeholder_parent; //place hair as child to head gameobject/transform
    [SerializeField]
    public GameObject currentHair;
    [SerializeField]
    public Texture2D currentHaircap;
    [SerializeField]
    public Color currentColor = Color.black;
    [SerializeField]
    public GameObject didimoPlaceholderParent;

    [SerializeField]
    protected GameObject configHairPanel;

    [SerializeField]
    protected Sprite[] hairPreviews_headShape2;
    [SerializeField]
    protected Transform[] hairPrefabs_headShape2;
    [SerializeField]
    protected Texture2D[] haircapTextures_headShape2;

    private GameCoroutineManager coroutineManager;

    protected string didimoCode = "";
    protected int current_hairstyle_index;
    public bool featureDisabled = false;

    public static int HAIRSTYLE_NUM_COLOR = 20;
    protected List<Color> hairColorList;

    protected virtual void Start()
    {
        coroutineManager = new GameCoroutineManager();
        if (featureDisabled)
            return;
        if (configHairPanel != null)
            configHairPanel.gameObject.SetActive(false);

        InitHairColorList();
    }

    protected virtual void OnEnable()
    {
        if (featureDisabled)
            return;
    }

    protected virtual void OnDisable()
    {
        if (featureDisabled)
            return;
    }

    public void InitHairColorList()
    {
        if (featureDisabled)
            return;

        List<string> hairColorNames = new List<string>();
        hairColorList = GetComponent<HairColorGalleryHorizontalScroller_v2>().GenerateColorList(ref hairColorNames);
    }


    /**************************************************************************/
    /**** UI event handlers ***************************************************/
    /**************************************************************************/
    public void ToggleConfigHairPanel()
    {
        if (featureDisabled)
            return;
        configHairPanel.SetActive(!configHairPanel.activeSelf);
    }
    public void ShowConfigHairPanel()
    {
        if (featureDisabled)
            return;
        configHairPanel.SetActive(true);
    }
    public void HideConfigHairPanel()
    {
        if (featureDisabled)
            return;
        configHairPanel.SetActive(false);
    }

    public void InitHairList(string didimoCode)
    {
        if (featureDisabled)
            return;
        this.didimoCode = didimoCode;
        //todo: create scroll list with preview buttons according to templateVersion
        //GetComponent<HairGalleryHorizontalScroller>().InitializeHairGalleryScroller();
        //apply default hair (bald) - todo: check user prefs and apply that instead - but that is not code for the base class
        //ChangeHairToIndex(0);
    }

    public Sprite[] GetHairPreviews()
    {
        return hairPreviews_headShape2;
    }

    public void ChangeHairToIndex(int index)
    {
        if (featureDisabled)
            return;
        current_hairstyle_index = index;
        ApplyHair(true);
    }

    public void ChangeHairColorToIndex(int index)
    {
        if (featureDisabled)
            return;
        ApplyHairColor(hairColorList[index]);
    }

    public void ChangeHairColorToIndex(int index, MaterialState matState)
    {
        if (featureDisabled)
            return;
        ApplyHairColor(hairColorList[index], matState);
    }

    public Transform GetHairPrefab(int index)
    {
        Debug.Log("GetHairPrefab index " + index); ;
        return hairPrefabs_headShape2[index];
    }

    public Texture2D GetHaircapTexture(int index)
    {
        return haircapTextures_headShape2[index];
    }

    public virtual void ApplyHair(bool shouldSaveRender)
    {
        //Debug.Log("ApplyHair");
        if (featureDisabled)
            return;
        CleanUpPreviousHair();
        Transform prefab = GetHairPrefab(current_hairstyle_index);
        if (prefab != null)
        {
            Color hairColor = currentColor;
            //Debug.Log("ApplyHair - prefab is not null - "+prefab.name);
            //apply haircap
            Texture2D haircapTexture = GetHaircapTexture(current_hairstyle_index);
            Transform parentGeometry = didimoPlaceholderParent.transform.FindRecursive("DidimoHeadSketching");// RootNode");
            if (parentGeometry == null)
                parentGeometry = didimoPlaceholderParent.transform.FindRecursive("RootNode");
            SkinnedMeshRenderer[] meshes = parentGeometry?.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (meshes != null)
            {
                foreach (SkinnedMeshRenderer mesh in meshes)
                {
                    //Debug.Log("ApplyHair - looking for face mesh to apply haircap - " + mesh.name);
                    if (mesh.name.ToLower().Contains("face"))
                    {
                        //Debug.Log("ApplyHair - applying haircap texture ("+haircapTexture.name+") on face mesh - " + mesh.name);
                        mesh.material.SetTexture("hairCapSampler", haircapTexture);
                        //also apply hairColor to haircap (todo: bugfix white color becoming transparent)
                        mesh.material.SetColor("hairColor", hairColor);
                        break;
                    }
                }
            }

            Debug.Log("ApplyHair - looking for head jnt transform to apply hair prefab");
            Transform headJointTransform = didimoPlaceholderParent.transform.FindRecursive("jnt_m_headEnd_001"); //new template
            if (headJointTransform != null)
            {
                Transform t = didimoPlaceholderParent.transform.FindRecursive("RootNode").parent;
                //Debug.Log("ApplyHair - head jnt transform to apply hair prefab found - "+ headJointTransform.name);
                //apply hair
                Transform currentHairTransform = Instantiate(prefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
                currentHairTransform.parent = t;// hair_placeholder_parent.transform;
                currentHairTransform.localPosition = new Vector3(0f, 0f, 0f);
                currentHairTransform.localRotation = Quaternion.identity;
                currentHairTransform.localScale = new Vector3(1f, 1f, 1f);
                currentHair = currentHairTransform.gameObject;
                DeformHair();
                currentHairTransform.parent = headJointTransform;
            }
            else
            {
                Debug.LogError("Failed to get 'jnt_m_headEnd_001'. Unable to apply hair.");
            }
            //apply hair color to mesh
            currentHair.GetComponentInChildren<Renderer>().material.SetColor("diffColor", hairColor);
        }
        //else Debug.Log("ApplyHair - prefab is null");
    }

    public void CleanUpPreviousHair()
    {
        //Debug.Log("CleanUpPreviousHair");
        if (featureDisabled)
            return;
        if (currentHair != null)
        {
            Destroy(currentHair);

            //clear haircap

            Transform parentGeometry = didimoPlaceholderParent.transform.FindRecursive("DidimoHeadSketching");// RootNode");
            if (parentGeometry == null)
                parentGeometry = didimoPlaceholderParent.transform.FindRecursive("RootNode");
            if (parentGeometry != null)
            {
                SkinnedMeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer mesh in meshes)
                {
                    //Debug.Log("ApplyHair - looking for face mesh to apply haircap - " + mesh.name);
                    if (mesh.name.ToLower().Contains("face"))
                    {
                        //Debug.Log("CleanUpPreviousHair - clearing haircap texture");
                        mesh.material.SetTexture("hairCapSampler", null);
                        break;
                    }
                }
            }
        }
        currentHair = null;
    }

    /**************************************************************************/
    /**** Hair color ****************************************************/
    /**************************************************************************/

    public void ApplyHairColor(Color newColor)
    {
        if (featureDisabled)
            return;

        if (currentHair != null)
        {
            //Debug.Log("ApplyHairColor " + newColor.ToString());
            currentHair.GetComponentInChildren<Renderer>().material.SetColor("diffColor", newColor);

            //apply new color to haircap
            //Texture2D haircapTexture = GetHaircapTexture(current_hairstyle_index);
            Transform parentGeometry = didimoPlaceholderParent.transform.FindRecursive("RootNode");
            SkinnedMeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer mesh in meshes)
            {
                //Debug.Log("ApplyHair - looking for face mesh to apply haircap - " + mesh.name);
                if (mesh.name.ToLower().Contains("face"))
                {
                    //Debug.Log("ApplyHairColor - applying haircap color (" + haircapTexture.name + ") on face mesh - " + mesh.name);
                    mesh.material.SetColor("hairColor", newColor);
                    break;
                }
            }

        }

        currentColor = newColor;
    }

    public void ApplyHairColor(Color newColor, MaterialState matState)
    {
        if (featureDisabled)
            return;

        if (currentHair != null)
        {
            Material mat = currentHair.GetComponentInChildren<Renderer>().material;

            //apply new color to haircap
            //Texture2D haircapTexture = GetHaircapTexture(current_hairstyle_index);
            Transform parentGeometry = didimoPlaceholderParent.transform.FindRecursive("RootNode");
            SkinnedMeshRenderer[] meshes = parentGeometry.GetComponentsInChildren<SkinnedMeshRenderer>();

            Material faceMaterial = null;
            foreach (SkinnedMeshRenderer mesh in meshes)
            {
                //Debug.Log("ApplyHair - looking for face mesh to apply haircap - " + mesh.name);
                if (mesh.name.ToLower().Contains("face"))
                {
                    //Debug.Log("ApplyHairColor - applying haircap color (" + haircapTexture.name + ") on face mesh - " + mesh.name);
                    //mesh.material.SetColor("hairColor", newColor);
                    faceMaterial = mesh.material;
                    break;
                }
            }

            // For now this is hardcoded, but should change in the future
            // material 0 is for the hair
            foreach (MaterialState.Material.Parameter parameters in matState.materials[0].parameters)
            {
                parameters.SetMaterialProperty(mat, null);
            }

            //material 1 is for thr skin
            foreach (MaterialState.Material.Parameter parameters in matState.materials[1].parameters)
            {
                parameters.SetMaterialProperty(faceMaterial, null);
            }
        }

        currentColor = newColor;
    }

    /**************************************************************************/
    /**** Hair deformation ****************************************************/
    /**************************************************************************/

    void DeformHair()
    {
        coroutineManager = new GameCoroutineManager();
        DeformHair(coroutineManager);
    }

    void DeformHair(CoroutineManager coroutineManager)
    {
        LoadingOverlay.Instance.ShowLoadingMenu(() =>
        {
            coroutineManager.StopAllCoroutines();
        }, "Generating hairstyle");

        byte[] vertexBytes = GetVertexPositionsArray();

        ServicesRequests servicesRequests = ServicesRequestsBase.GameInstance;
        servicesRequests.DeformDidimoAsset(
                  coroutineManager,
                  didimoCode,
                  currentHair.name,
                  vertexBytes,
                  (newVertexBytes) =>
                  {
                      //Debug.Log("DeformHair - response received");
                      UpdateVertexPositionsFromArray(newVertexBytes);
                      LoadingOverlay.Instance.Hide();
                  },
                  //System.Action<float> onProgressUpdateDelegate,
                  (progressFloat) =>
                  {
                      //Debug.Log("DeformHair - progress:"+progressFloat);
                      LoadingOverlay.Instance.ShowProgress(progressFloat);
                      if (progressFloat == 100)
                          LoadingOverlay.Instance.ShowLoadingMenu(() =>
                          {
                              coroutineManager.StopAllCoroutines();
                          }, "Loading hairstyle");
                  },
                  exception =>
                  {
                      Debug.LogError("DeformHair - exception:" + exception.Message);
                      LoadingOverlay.Instance.Hide();
                      coroutineManager.StopAllCoroutines();
                      ErrorOverlay.Instance.ShowError(exception.Message);
                  });
    }

    byte[] GetVertexPositionsArray()
    {
        byte[] vertex_bytes = new byte[0];
        try
        {
            Stream stream = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                // Unity uses meters, but on the pipeline we use centimeters
                float scaleFactor = 0.01f;
                MeshFilter[] meshList = currentHair.GetComponentsInChildren<MeshFilter>();
                Vector3[] vertexList;
                int byte_length = 0;
                int totalLength = 0;

                foreach (MeshFilter mf in meshList)
                {
                    totalLength += 3 * 4 * mf.sharedMesh.vertexCount;
                }

                Array.Resize(ref vertex_bytes, totalLength);

                foreach (MeshFilter mf in meshList)
                {
                    vertexList = mf.mesh.vertices;
                    for (int i = 0; i < vertexList.Length; i++)
                    {
                        vertexList[i] /= scaleFactor;
                        // In unity, the x coordinate is inverted
                        InsertAsBytes(vertex_bytes, ref byte_length, vertexList[i].x * -1);
                        InsertAsBytes(vertex_bytes, ref byte_length, vertexList[i].y);
                        InsertAsBytes(vertex_bytes, ref byte_length, vertexList[i].z);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
        return vertex_bytes;
    }

    void InsertAsBytes(byte[] array, ref int position, float value)
    {
        byte[] byteValue = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(byteValue); // Convert big endian to little endian
        }

        byteValue.CopyTo(array, position);
        position += byteValue.Length;
    }

    void UpdateVertexPositionsFromArray(byte[] newVertexBytes)
    {
        MeshFilter[] meshList = currentHair.GetComponentsInChildren<MeshFilter>();
        int arrayPosition = 0;
        foreach (MeshFilter mf in meshList)
        {
            Vector3[] vertexList = new Vector3[mf.mesh.vertexCount];
            //vertexList = mf.mesh.vertices;
            for (int i = 0; i < mf.mesh.vertexCount; i++)
            {
                SetVector3FromNextBytes(newVertexBytes, ref arrayPosition, ref vertexList[i]);
            }
            mf.mesh.vertices = vertexList;
        }
    }

    public void SetVector3FromNextBytes(byte[] bytes, ref int position, ref Vector3 result)
    {
        // Unity uses meters, but on the pipeline we use centimeters
        float scaleFactor = 0.01f;

        byte[] value = new byte[4];
        Array.Copy(bytes, position, value, 0, 4);
        // In unity, the x coordinate is inverted
        result.x = ConvertFloat(value) * -1;
        position += 4;
        Array.Copy(bytes, position, value, 0, 4);
        result.y = ConvertFloat(value);
        position += 4;
        Array.Copy(bytes, position, value, 0, 4);
        result.z = ConvertFloat(value);
        position += 4;
        result *= scaleFactor;
    }

    public float ConvertFloat(byte[] bytes)
    {
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes); // Convert big endian to little endian
        }
        float myFloat = BitConverter.ToSingle(bytes, 0);
        return myFloat;
    }
}
