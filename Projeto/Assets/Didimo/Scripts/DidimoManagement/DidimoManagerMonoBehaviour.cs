using UnityEngine;

namespace Didimo.DidimoManagement
{
    /// <summary>
    /// An abstract Monobehaviour that requires the DidimoImporter component.
    /// Monobehaviours that will handle the menu and require the DidimoImporter class should derive from this class, and have the same parent as the <see cref="DidimoImporter"/> monobehaviour.
    /// </summary>
    [RequireComponent(typeof(DidimoImporter))]
    public abstract class DidimoManagerMonoBehaviour : MonoBehaviour
    {
        DidimoImporter _didimoImporter;

        public DidimoImporter didimoImporter
        {
            get
            {
                if (_didimoImporter == null)
                {
                    _didimoImporter = GetComponent<DidimoImporter>();
                }
                return _didimoImporter;
            }
        }
    }
}