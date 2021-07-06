using UnityEngine;

namespace Didimo.DidimoManagement
{
    public partial class DidimoLoader : DidimoLoaderBase
    {
    }

    /// <summary>
    /// Monobehaviour used to update the mesh vertices and normals. For that, it uses the data from the json file.
    /// It also updates the skin texture and normal map.
    /// Attach this Monobehaviour to the game object that you wish to manage. Requires the game object to have a SkinnedMeshRenderer property.
    /// See <see cref="UpdateModel"/> method.
    /// </summary>
    //[RequireComponent(typeof(SkinnedMeshRenderer))]
    public class DidimoLoaderBase : MonoBehaviour
    {
        /// <summary>
        /// Update didimo model
        /// </summary>
        /// <param name="model">The model, parsed from the didimo.json file outputed by the didimo pipeline.</param>
        /// <param name="createNew">If true, we will instantiate a new didimo, instead of updating an existing one.</param>
        public void UpdateModel(IDidimoModel didimoModel, string assetsPath, bool createNew = false)
        {
            if (!createNew)
            {
                foreach (Transform child in transform)
                {
                    Destroy(child);
                }

                didimoModel.InstantiateDidimo(transform, assetsPath, !createNew);
            }
        }

    }
}