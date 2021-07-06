using UnityEditor;
using UnityEngine;

namespace Didimo.Animation.Editor
{
    [CustomEditor(typeof(CircularTester))]
    public class CircularBufferWindow : UnityEditor.Editor
    {
        private CircularTester tester;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Run"))
            {
                tester.TestBuffer();
            }
        }


        public void OnEnable()
        {
            tester = (CircularTester)target;
        }
    }
}