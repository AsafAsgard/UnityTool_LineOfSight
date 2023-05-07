using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace LOS
{
    [CreateAssetMenu()]
    public class CentricMesh3D : MeshDrawer
    {
        public override void Draw(in Vector3[,] meshPoints)
        {
            if (mesh == null || refTransform == null) return;
            mesh.Clear();
            int rows = meshPoints.GetLength(0);
            int columns = meshPoints.GetLength(1);

            Vector3[] vertices = new Vector3[rows * columns];
            int[] triangles = new int[(rows - 1) * (columns - 1) * 6];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    vertices[i * columns + j] = refTransform.InverseTransformPoint(meshPoints[i, j]);
                }
            }
            int index = 0;
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    int bottomLeft = i * columns + j;
                    int bottomRight = i * columns + j + 1;
                    int topLeft = (i + 1) * columns + j;
                    int topRight = (i + 1) * columns + j + 1;

                    triangles[index++] = topLeft;
                    triangles[index++] = bottomLeft;
                    triangles[index++] = bottomRight;

                    triangles[index++] = topLeft;
                    triangles[index++] = bottomRight;
                    triangles[index++] = topRight;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }
        public void OnDrawGizmos()
        {
            if (mesh == null) return;
            Gizmos.color = Color.white;
            Gizmos.DrawMesh(mesh);
        }


    }

    
}