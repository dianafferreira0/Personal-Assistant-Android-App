using System.IO;
using Didimo.Animation.DataObjects;
using UnityEditor;
using UnityEngine;

namespace Didimo.Animation.Editor
{
    [CustomEditor(typeof(Animation.RealtimeRig))]
    public class RealtimeRigEditor : UnityEditor.Editor
    {
        public Mesh mesh;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Animation.RealtimeRig realtimeRig = (Animation.RealtimeRig)target;

            if (GUILayout.Button("Import Rig"))
            {
                string rigPath = EditorUtility.OpenFilePanel("Select Didimo Realtime Rig", "", "json");
                if (!string.IsNullOrEmpty(rigPath))
                {
                    StreamReader reader = new StreamReader(rigPath);

                    RealtimeRigDataObject dataObject = RealtimeRigDataObject.LoadFromJson<RealtimeRigDataObject>(reader.ReadToEnd());

                    realtimeRig.Build(dataObject);
                }
            }
        }
    }
}