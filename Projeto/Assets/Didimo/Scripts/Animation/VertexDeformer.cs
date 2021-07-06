using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Animation
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class VertexDeformer : MonoBehaviour
    {
        public float maxVertexOffset = 0.001f;
        public Vector3 headOffset;
        public MeshFilter target;
        public SkinnedMeshRenderer targetSMR;
        [SerializeField, HideInInspector]
        //// Map indices of vertices of current mesh, into target mesh
        private List<int> vertexMap;

        public bool incremental = true;

        //private Vector3[] originalVertexPos;
        private SkinnedMeshRenderer skinnedMeshRenderer;

        int GetClosestVertexIndex(Vector3 vertexPos)
        {
            int closestIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < targetVertices.Length; i++)
            {
                if (Vector3.Distance(vertexPos, targetVertices[i]) < minDistance)
                {
                    minDistance = Vector3.Distance(vertexPos, targetVertices[i]);
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        public void UpdateVertexMap()
        {
            vertexMap = new List<int>();

            Mesh mesh = new Mesh();
            if (target != null)
            {
                targetVertices = target.sharedMesh.vertices;
            }
            else
            {
                targetSMR.BakeMesh(mesh);
                targetVertices = mesh.vertices;
            }

            GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
            sourceVertices = mesh.vertices;
            int remappedVertices = 0;

            for (int i = 0; i < sourceVertices.Length; i++)
            {
                //if (Vector3.Distance(sourceVertices[i] + headOffset, targetVertices[i]) > maxVertexOffset)
                //{
                //    vertexMap.Add(GetClosestVertexIndex(sourceVertices[i] + headOffset));
                //    remappedVertices++;
                //}
                //else
                //{
                vertexMap.Add(i);
                //}
            }
            Debug.Log("Remapped a total of " + remappedVertices + " vertices.");
        }

        private void Start()
        {
            m = new Mesh();
            mesh2 = new Mesh();
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            //originalVertexPos = skinnedMeshRenderer.sharedMesh.vertices;
            if (target != null)
            {
                target.sharedMesh = Instantiate(target.sharedMesh);
                target.mesh = Instantiate(target.mesh);
            }
            else
            {
                targetSMR.sharedMesh = Instantiate(targetSMR.sharedMesh);
            }

            if (vertexMap.Count != skinnedMeshRenderer.sharedMesh.vertexCount)
            {
                Debug.LogError("Please Cache the Vertex Map on the VertexDeformer");
                enabled = false;
            }

            if (target == null)
            {
                List<float> weights = new List<float>();
                for (int i = 0; i < targetSMR.sharedMesh.blendShapeCount; i++)
                {
                    weights.Add(targetSMR.GetBlendShapeWeight(i));
                    targetSMR.SetBlendShapeWeight(i, 0);
                }

                targetSMR.BakeMesh(mesh2);

                for (int i = 0; i < targetSMR.sharedMesh.blendShapeCount; i++)
                {
                    targetSMR.SetBlendShapeWeight(i, weights[i]);
                }
            }

            if (incremental)
            {
                if (target != null)
                {
                    initialTargetVertices = target.mesh.vertices;
                }
                else
                {
                    initialTargetVertices = mesh2.vertices;
                    initialTargetNormals = mesh2.normals;
                }
                skinnedMeshRenderer.BakeMesh(m);
                initialSourceVertices = m.vertices;
            }
        }

        Vector3[] initialTargetVertices;
        Vector3[] initialTargetNormals;
        Vector3[] initialSourceVertices;
        Vector3[] targetVertices;
        //Vector3[] targetNormals;
        Vector3[] sourceVertices;
        //Vector3[] sourceNormals;
        Mesh m;
        Mesh mesh2;

        // Do this during update, before on the target is performed
        private void LateUpdate()
        {
            skinnedMeshRenderer.BakeMesh(m);

            if (target != null)
            {
                targetVertices = target.mesh.vertices;
                //targetNormals = target.mesh.normals;
            }
            else
            {
                targetSMR.sharedMesh.vertices = initialTargetVertices;
                targetSMR.sharedMesh.normals = initialTargetNormals;
                //targetSMR.BakeMesh(mesh2);
                targetVertices = mesh2.vertices;
                //targetNormals = mesh2.normals;
            }
            sourceVertices = m.vertices;
            //sourceNormals = m.normals;

            Vector3 vertexOffset;
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                if (incremental)
                {
                    vertexOffset = sourceVertices[i] - initialSourceVertices[i];
                    if (vertexOffset.magnitude > 0.0001)
                    {
                        targetVertices[vertexMap[i]] = initialTargetVertices[vertexMap[i]] + vertexOffset;
                    }
                }
                else
                {
                    targetVertices[vertexMap[i]] = sourceVertices[i] + headOffset;
                }
                //targetNormals[vertexMap[i]] = sourceNormals[i];
            }

            if (target != null)
            {
                target.mesh.vertices = targetVertices;
                //target.mesh.normals = targetNormals;
            }
            else
            {
                targetSMR.sharedMesh.vertices = targetVertices;
                //targetSMR.sharedMesh.normals = targetNormals;
            }
        }
    }
}
