using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentricMesh3D : MeshDrawer
{
    public override void Draw(Transform refTransform, LineOfSightParameters parameters)
    {

        
        float verticalAngle = parameters.verticalAngle;
        float horizontalAngle = parameters.horizontalAngle;

        float maxViewDistance = parameters.enviromentDetection.distance;

        int numTriangles = (2 * verticalSegments + 2) * horizontalSegments;
        if (verticalAngle < 90)
            numTriangles += verticalSegments * 2;
        if (horizontalAngle < 180)
            numTriangles += verticalSegments * 2 + 4;

        int numVertices = numTriangles * 3;
        Vector3[] veritcies = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        /// Newton intepolation on the bias values
        /// [0      0.75    0.5     0.25    0.125   0]
        /// [45     51.429  60      72      80      90]

        float verticalLimit = -12.2197f * Mathf.Pow(Mathf.Abs(verticalBias), 5) + 44.2453f * Mathf.Pow(Mathf.Abs(verticalBias), 4) - 74.6987f * Mathf.Pow(Mathf.Abs(verticalBias), 3) + 87.5307f * Mathf.Pow(Mathf.Abs(verticalBias), 2) - 89.8576f * Mathf.Abs(verticalBias) + 90f;
        verticalAngle = Mathf.Clamp(verticalAngle, 0, verticalLimit);

        Vector3 center = Vector3.zero;

        int vert = 0;
        float deltaHorizontalAngle = (horizontalAngle * 2) / horizontalSegments;
        float deltaVerticalAngle = (verticalAngle * 2) / verticalSegments; //add 2 for the top and bottom triangles
        float currentVerticalAngle = -verticalAngle + verticalBias * verticalAngle;


        //ChatGPT
        //Vector3[] veritcies = new Vector3[(horizontalSegments + 1) * (verticalSegments + 1)];
        //int vert = 0;
        //Vector3 center = Vector3.zero;

        //float deltaVerticalAngle = (2 * verticalAngle) / verticalSegments;
        //float deltaHorizontalAngle = (2 * horizontalAngle) / horizontalSegments;

        //for (int verticalIndex = 0; verticalIndex <= verticalSegments; verticalIndex++)
        //{
        //    float currentVerticalAngle = -verticalAngle + (verticalIndex * deltaVerticalAngle);
        //    for (int horizontalIndex = 0; horizontalIndex <= horizontalSegments; horizontalIndex++)
        //    {
        //        float currentHorizontalAngle = -horizontalAngle + (horizontalIndex * deltaHorizontalAngle);
        //        Vector3 point = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxViewDistance;
        //        if (showDetailedScan)
        //        {
        //            point = TestPoint(refTransform.position, point, maxViewDistance, parameters.enviromentDetection.layer);
        //        }
        //        veritcies[vert++] = point;
        //    }
        //}

        for (int verticalIndex = 0; verticalIndex <= verticalSegments; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
        {
            float currentHorizontalAngle = -horizontalAngle;
            for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
            {
                Vector3 topLeft = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxViewDistance;
                Vector3 topRight = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxViewDistance;
                Vector3 bottomLeft = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxViewDistance;
                Vector3 bottomRight = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxViewDistance;
                /// Dynamic mode
                /// Todo: Fix bad performance
                if (showDetailedScan)
                {
                    topLeft = TestPoint(refTransform.position, topLeft, maxViewDistance, parameters.enviromentDetection.layer);
                    topRight = TestPoint(refTransform.position, topRight, maxViewDistance, parameters.enviromentDetection.layer);
                    bottomLeft = TestPoint(refTransform.position, bottomLeft, maxViewDistance, parameters.enviromentDetection.layer);
                    bottomRight = TestPoint(refTransform.position, bottomRight, maxViewDistance, parameters.enviromentDetection.layer);
                }

                #region Top Side
                //Top side
                if (Mathf.Abs(currentVerticalAngle - (-verticalAngle + verticalBias * verticalAngle)) < .01f)
                {
                    if (verticalAngle < 90)
                    {
                        veritcies[vert++] = center;
                        veritcies[vert++] = topLeft;
                        veritcies[vert++] = topRight;
                    }

                }
                #endregion

                #region Bottom Side
                //Bottom side
                if (Mathf.Abs(currentVerticalAngle - (verticalAngle + verticalBias * verticalAngle)) < .01f)
                {

                    //bottom
                    if (verticalAngle < 90)
                    {
                        veritcies[vert++] = center;
                        veritcies[vert++] = topRight;
                        veritcies[vert++] = topLeft;
                    }


                    continue;
                }
                #endregion

                //far side
                veritcies[vert++] = bottomLeft;
                veritcies[vert++] = bottomRight;
                veritcies[vert++] = topRight;

                veritcies[vert++] = topRight;
                veritcies[vert++] = topLeft;
                veritcies[vert++] = bottomLeft;

                if (horizontalAngle < 180 && Mathf.Abs(currentHorizontalAngle - (-horizontalAngle)) < .01f)
                {
                    //left side
                    veritcies[vert++] = center;
                    veritcies[vert++] = bottomLeft;
                    veritcies[vert++] = topLeft;
                }
                if (horizontalAngle < 180 && Mathf.Abs(currentHorizontalAngle - (horizontalAngle - deltaHorizontalAngle)) < .01f)
                {
                    //right side
                    veritcies[vert++] = center;
                    veritcies[vert++] = topRight;
                    veritcies[vert++] = bottomRight;
                }
            }
        }


        for (int i = 0; i < numVertices; i++)
        {
            triangles[i] = i;
        }

        Vector2[] uvs = new Vector2[veritcies.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(veritcies[i].x, veritcies[i].z);
        }


        mesh.vertices = veritcies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
    }
}
