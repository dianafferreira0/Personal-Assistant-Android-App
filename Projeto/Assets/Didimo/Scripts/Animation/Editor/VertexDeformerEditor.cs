using UnityEditor;
using UnityEngine;

namespace Didimo.Animation.Editor
{
    [CustomEditor(typeof(VertexDeformer))]
    public class VertexDeformerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VertexDeformer vertexDeformer = (VertexDeformer)target;
            if (GUILayout.Button("Cache Vertex Map"))
            {
                vertexDeformer.UpdateVertexMap();
            }
        }
    }
}