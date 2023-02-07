using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


using Random = UnityEngine.Random;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LOSModule : MonoBehaviour
{
    public string FieldName { get; } = "LOS";

    [Header("Appearance")]
    [SerializeField] public bool centric = true;
    public bool showDetailedScan = false;
    [SerializeField, Tooltip("Control the level of detail of the mesh")]
    [Range(1, 10)]
    public int subDivision = 3;
    [SerializeField, Tooltip("Allow dynamic mesh to be created once")]
    private bool bakeMesh = false;


    [SerializeField] public float maxviewDistance = 20;
    [SerializeField] public float minviewDistance = 0;
    [SerializeField][Range(-1f, 1f)] public float verticalBias;
    [SerializeField][Range(1, 180)] public float horizontalAngle = 25;
    [SerializeField][Range(1, 90)] public float verticalAngle = 25;


    [Header("Scan Options")]
    [SerializeField] private float scanFrequency = 30;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask enviromentLayers;


    [Header("Noise Options")]
    [SerializeField] private float noiseRadius = 0;
    [SerializeField] private float noiseMinDistance = 0;
    //[SerializeField] private float errorChance = 0f;
    
    [Header("Debug")]
    [SerializeField] public bool enable2dMesh = false;
    [SerializeField] public bool showTargetLines = false;

    //[SerializeField] LineOfSightParameters lineOfSightParameters;


    private Mesh mesh;
    private Material lineMaterial;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private static List<GameObject> poiObjects;

    private List<GameObject> poiTargets;
    private bool isScanning = false;
    private bool multiLosMode = false;

    private float scanInterval;
    private float scanTimer;
    private bool shouldRefreshMesh = false;
    private bool isBaked = false;



    public List<GameObject> GetTargets() => poiTargets;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "LOS";

        isScanning = false;

        poiTargets = new List<GameObject>();

        CreateLineMaterial();

        //viewDistance = lineOfSightParameters.viewRange;
        //horizontalAngle = lineOfSightParameters.horizontalAngle / 2;
        //verticalAngle = lineOfSightParameters.verticalAngle / 2;
        //noiseRadius = lineOfSightParameters.noiseRadius;
        //noiseMinDistance = lineOfSightParameters.noiseMinDistance;
        //errorChance = lineOfSightParameters.visualError / 100.0f;   //0 to 1

        //Initialize mesh
        shouldRefreshMesh = true;


        //#if UNITY_EDITOR
        //        meshRenderer.sharedMaterial = meshMaterial;
        //#else
        //                meshRenderer.material = meshMaterial;
        //#endif
        meshRenderer.enabled = true;

    }
    
    public void SetMultiLos()
    {
        multiLosMode = true;
    }
    void Start()
    {
        SetDetailedScanMesh(showDetailedScan || bakeMesh);
        scanInterval = 1.0f / scanFrequency;
    }

    void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0 && !isScanning)
        {
            scanTimer += scanInterval;
            UpdateMesh();
            Scan();
        }

    }

    public void RefreshMesh()
    {
        shouldRefreshMesh = true;
    }

    private void UpdateMesh()
    {
        if (shouldRefreshMesh)
        {
            shouldRefreshMesh = false;
            BuildLosMesh();
            return;
        }
        if (!showDetailedScan) return;
        if (bakeMesh && isBaked) return;
        BuildLosMesh();

    }

    private void BuildLosMesh()
    {
        if (mesh != null)
            mesh.Clear();
        CreateLOSMesh();
        meshFilter.mesh = mesh;
    }

    [ContextMenu("Scan")]
    private void Scan()
    {
        isScanning = true;
        if (poiObjects == null)
            poiObjects = new List<GameObject>();

        GameObject[] pois = Physics.OverlapSphere(transform.position, maxviewDistance, targetLayers).Select(o => o.gameObject).ToArray();
        poiTargets.Clear();
        poiTargets = pois.Where(poi => TargetInSight(poi.transform.position)).ToList();
        isScanning = false;
    }


    private bool TargetInSight(Vector3 targetPos)
    {
        //Can be seen
        if (Physics.Linecast(base.transform.position, targetPos + Vector3.up * .01f, enviromentLayers)) return false;

        //Is in range
        float distanceToTarget = Vector3.Distance(base.transform.position, targetPos);
        if (distanceToTarget > maxviewDistance || distanceToTarget < minviewDistance) return false;

        //Validate angles
        Quaternion angleToTarget = Quaternion.FromToRotation(transform.forward, targetPos - transform.position);

        float targetHorizontalAngle = angleToTarget.eulerAngles.y;
        if (targetHorizontalAngle > 180)
            targetHorizontalAngle -= 360;
        float targetVerticalAngle = angleToTarget.eulerAngles.x;
        if (targetVerticalAngle > 180)
            targetVerticalAngle -= 360;
        if (Mathf.Abs(targetHorizontalAngle) > horizontalAngle) return false;
        if (Mathf.Abs(targetVerticalAngle) > verticalAngle) return false;

        return true;

    }

    #region Mesh Handling
    private void CreateLOSMesh()
    {
        if (mesh == null) return;
        mesh.Clear(false);

        int verticalSegments = 4 * subDivision;
        int horizontalSegments = 4 * subDivision;

        if (enable2dMesh)
        {
            if (minviewDistance > 0)
                Draw2DHollowMesh(horizontalSegments);
            else if (centric)
                Draw2DCentricMesh(horizontalSegments);
        }
        else if (minviewDistance > 0)
            DrawHollowMesh(verticalSegments, horizontalSegments);
        else if (centric)
            DrawCentricMesh(verticalSegments, horizontalSegments);
        else
            DrawSphericMesh(verticalSegments, horizontalSegments);

        mesh.RecalculateNormals();
        if(bakeMesh && !isBaked)
            isBaked = true;
    }

    #region 2D
    private void Draw2DHollowMesh(int horizontalSegments)
    {
        int numTriangles = 2 * horizontalSegments;


        int numVertices = numTriangles * 3;
        Vector3[] veritcies = new Vector3[numVertices];
        int[] triangles = new int[numVertices];


        Vector3 center = TestPoint(Vector3.zero - transform.up * maxviewDistance);

        int vert = 0;
        float deltaHorizontalAngle = (horizontalAngle * 2) / horizontalSegments;


        float currentHorizontalAngle = -horizontalAngle;
        for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
        {
            Vector3 outterBottomLeft = Quaternion.Euler(0, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
            Vector3 outterBottomRight = Quaternion.Euler(0, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;

            Vector3 innerBottomLeft = Quaternion.Euler(0, currentHorizontalAngle, 0) * Vector3.forward * minviewDistance;
            Vector3 innerBottomRight = Quaternion.Euler(0, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * minviewDistance;

            /// Dynamic mode
            /// Todo: Fix bad performance
            if (showDetailedScan)
            {
                outterBottomLeft = Test2DPoint(center, outterBottomLeft);
                outterBottomRight = Test2DPoint(center, outterBottomRight);

                innerBottomLeft = Test2DPoint(center, innerBottomLeft);
                innerBottomRight = Test2DPoint(center, innerBottomRight);
            }

            #region Bottom Side

            if (verticalAngle < 90)
            {
                veritcies[vert++] = innerBottomLeft;
                veritcies[vert++] = outterBottomLeft;
                veritcies[vert++] = outterBottomRight;

                veritcies[vert++] = innerBottomLeft;
                veritcies[vert++] = outterBottomRight;
                veritcies[vert++] = innerBottomRight;
            }


            continue;

            #endregion
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
    private void Draw2DCentricMesh(int horizontalSegments)
    {

        int numTriangles = horizontalSegments;


        int numVertices = numTriangles * 3;
        Vector3[] veritcies = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        Vector3 center = TestPoint(Vector3.zero - transform.up * maxviewDistance);

        int vert = 0;
        float deltaHorizontalAngle = (horizontalAngle * 2) / horizontalSegments;

        float currentHorizontalAngle = -horizontalAngle;
        for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
        {
            Vector3 bottomLeft = Quaternion.Euler(0, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
            Vector3 bottomRight = Quaternion.Euler(0, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
            /// Dynamic mode
            /// Todo: Fix bad performance
            if (showDetailedScan)
            {
                bottomLeft = Test2DPoint(center, bottomLeft);
                bottomRight = Test2DPoint(center, bottomRight);
            }
            #region Bottom Side
            //Bottom side

            //bottom
            if (verticalAngle < 90)
            {
                veritcies[vert++] = center;
                veritcies[vert++] = bottomLeft;
                veritcies[vert++] = bottomRight;
            }
            #endregion
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

    #endregion
    #region 3D
    private void DrawHollowMesh(int verticalSegments, int horizontalSegments)
    {
        int numTriangles = 2 * ((2 * verticalSegments + 2) * horizontalSegments);
        if (verticalAngle < 90)
            numTriangles += verticalSegments * 4;
        if (horizontalAngle < 180)
            numTriangles += verticalSegments * 4 + 4;

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


        for (int verticalIndex = 0; verticalIndex <= verticalSegments; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
        {
            float currentHorizontalAngle = -horizontalAngle;
            for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
            {
                Vector3 outterTopLeft = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 outterTopRight = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 outterBottomLeft = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 outterBottomRight = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;

                Vector3 innerTopLeft = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * minviewDistance;
                Vector3 innerTopRight = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * minviewDistance;
                Vector3 innerBottomLeft = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * minviewDistance;
                Vector3 innerBottomRight = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * minviewDistance;

                /// Dynamic mode
                /// Todo: Fix bad performance
                if (showDetailedScan)
                {
                    outterTopLeft = TestPoint(outterTopLeft);
                    outterTopRight = TestPoint(outterTopRight);
                    outterBottomLeft = TestPoint(outterBottomLeft);
                    outterBottomRight = TestPoint(outterBottomRight);

                    innerTopLeft = TestPoint(innerTopLeft);
                    innerTopRight = TestPoint(innerTopRight);
                    innerBottomLeft = TestPoint(innerBottomLeft);
                    innerBottomRight = TestPoint(innerBottomRight);
                }

                #region Top Side
                //Top side
                if (Mathf.Abs(currentVerticalAngle - (-verticalAngle + verticalBias * verticalAngle)) < .01f)
                {
                    if (verticalAngle < 90)
                    {
                        veritcies[vert++] = innerTopLeft;
                        veritcies[vert++] = outterTopLeft;
                        veritcies[vert++] = outterTopRight;

                        veritcies[vert++] = innerTopLeft;
                        veritcies[vert++] = outterTopRight;
                        veritcies[vert++] = innerTopRight;
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
                        veritcies[vert++] = innerTopLeft;
                        veritcies[vert++] = outterTopRight;
                        veritcies[vert++] = outterTopLeft;

                        veritcies[vert++] = innerTopLeft;
                        veritcies[vert++] = innerTopRight;
                        veritcies[vert++] = outterTopRight;
                    }


                    continue;
                }
                #endregion


                //inner side
                veritcies[vert++] = innerBottomRight;
                veritcies[vert++] = innerBottomLeft;
                veritcies[vert++] = innerTopRight;

                veritcies[vert++] = innerBottomLeft;
                veritcies[vert++] = innerTopLeft;
                veritcies[vert++] = innerTopRight;

                //outter side
                veritcies[vert++] = outterBottomLeft;
                veritcies[vert++] = outterBottomRight;
                veritcies[vert++] = outterTopRight;

                veritcies[vert++] = outterTopRight;
                veritcies[vert++] = outterTopLeft;
                veritcies[vert++] = outterBottomLeft;

                if (horizontalAngle < 180 && Mathf.Abs(currentHorizontalAngle - (-horizontalAngle)) < .01f)
                {
                    //left side
                    veritcies[vert++] = innerTopLeft;
                    veritcies[vert++] = outterBottomLeft;
                    veritcies[vert++] = outterTopLeft;

                    veritcies[vert++] = innerTopLeft;
                    veritcies[vert++] = innerBottomLeft;
                    veritcies[vert++] = outterBottomLeft;
                }
                if (horizontalAngle < 180 && Mathf.Abs(currentHorizontalAngle - (horizontalAngle - deltaHorizontalAngle)) < .01f)
                {
                    //right side
                    veritcies[vert++] = outterTopRight;
                    veritcies[vert++] = outterBottomRight;
                    veritcies[vert++] = innerTopRight;

                    veritcies[vert++] = innerBottomRight;
                    veritcies[vert++] = innerTopRight;
                    veritcies[vert++] = outterBottomRight;
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

    private void DrawCentricMesh(int verticalSegments, int horizontalSegments)
    {

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


        for (int verticalIndex = 0; verticalIndex <= verticalSegments; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
        {
            float currentHorizontalAngle = -horizontalAngle;
            for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
            {
                Vector3 topLeft = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 topRight = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 bottomLeft = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                Vector3 bottomRight = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance;
                /// Dynamic mode
                /// Todo: Fix bad performance
                if (showDetailedScan)
                {
                    topLeft = TestPoint(topLeft);
                    topRight = TestPoint(topRight);
                    bottomLeft = TestPoint(bottomLeft);
                    bottomRight = TestPoint(bottomRight);
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


    private void DrawSphericMesh(int verticalSegments, int horizontalSegments)
    {
        int numTriangles = (2 * verticalSegments + 2) * horizontalSegments;
        if (verticalAngle < 90)
            numTriangles += verticalSegments * 2;
        if (horizontalAngle < 180)
            numTriangles += verticalSegments * 2 + 4;

        int numVertices = numTriangles * 3;
        Vector3[] veritcies = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        verticalAngle = Mathf.Clamp(verticalAngle, 0, 90 - Mathf.Abs(verticalBias * 45));
        Vector3 bias = verticalBias * Mathf.Sin(Mathf.Deg2Rad * verticalAngle) * maxviewDistance * Vector3.up;

        Vector3 center = Vector3.zero + bias;

        int vert = 0;
        float deltaHorizontalAngle = (horizontalAngle * 2) / horizontalSegments;
        float deltaVerticalAngle = (verticalAngle * 2) / (verticalSegments); //add 2 for the top and bottom triangles
        float currentVerticalAngle = -verticalAngle;


        for (int verticalIndex = 0; verticalIndex <= verticalSegments; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
        {
            float currentHorizontalAngle = -horizontalAngle;
            for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
            {
                Vector3 topLeft = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance + bias;
                Vector3 topRight = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance + bias;
                Vector3 bottomLeft = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward * maxviewDistance + bias;
                Vector3 bottomRight = Quaternion.Euler(currentVerticalAngle + deltaVerticalAngle, currentHorizontalAngle + deltaHorizontalAngle, 0) * Vector3.forward * maxviewDistance + bias;
                if (showDetailedScan)
                {
                    topLeft = TestPoint(topLeft);
                    topRight = TestPoint(topRight);
                    bottomLeft = TestPoint(bottomLeft);
                    bottomRight = TestPoint(bottomRight);
                }
                #region Top Side
                //Top side
                if (Mathf.Approximately(currentVerticalAngle, -verticalAngle))
                {
                    if (verticalAngle < 90)
                    {
                        veritcies[vert++] = center + Mathf.Sin(Mathf.Deg2Rad * verticalAngle) * maxviewDistance * Vector3.up;
                        veritcies[vert++] = topLeft;
                        veritcies[vert++] = topRight;
                    }



                    //top side triangles
                    if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, horizontalAngle - deltaHorizontalAngle))
                    {
                        //right
                        veritcies[vert++] = center;
                        veritcies[vert++] = center + Mathf.Sin(Mathf.Deg2Rad * verticalAngle) * maxviewDistance * Vector3.up;
                        veritcies[vert++] = topRight;
                    }
                    if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, -horizontalAngle))
                    {
                        //left
                        veritcies[vert++] = center + Mathf.Sin(Mathf.Deg2Rad * verticalAngle) * maxviewDistance * Vector3.up;
                        veritcies[vert++] = center;
                        veritcies[vert++] = topLeft;
                    }
                    //continue;
                }
                #endregion

                #region Bottom Side
                //Bottom side
                if (Mathf.Approximately(currentVerticalAngle, verticalAngle))
                {

                    //bottom side triangles
                    if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, horizontalAngle - deltaHorizontalAngle))
                    {
                        //right
                        veritcies[vert++] = center - Mathf.Sin(Mathf.Deg2Rad * currentVerticalAngle) * maxviewDistance * Vector3.up;
                        veritcies[vert++] = center;
                        veritcies[vert++] = topRight;
                    }
                    if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, -horizontalAngle))
                    {
                        //left
                        veritcies[vert++] = center;
                        veritcies[vert++] = center - Mathf.Sin(Mathf.Deg2Rad * currentVerticalAngle) * maxviewDistance * Vector3.up;
                        veritcies[vert++] = topLeft;
                    }

                    //bottom
                    if (verticalAngle < 90)
                    {
                        veritcies[vert++] = center - Mathf.Sin(Mathf.Deg2Rad * currentVerticalAngle) * maxviewDistance * Vector3.up;
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

                if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, -horizontalAngle))
                {
                    //left side
                    veritcies[vert++] = center;
                    veritcies[vert++] = bottomLeft;
                    veritcies[vert++] = topLeft;
                }
                if (horizontalAngle < 180 && Mathf.Approximately(currentHorizontalAngle, horizontalAngle - deltaHorizontalAngle))
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
    #endregion

    private Vector3 TestPoint(Vector3 point)
    {

        Ray ray = new Ray(base.transform.position, base.transform.TransformPoint(point) - base.transform.position);
        if (Physics.Raycast(ray, out RaycastHit hit, maxviewDistance, enviromentLayers))
        {
            //Debug.DrawLine(base.transform.position, hit.point);
            return base.transform.InverseTransformPoint(hit.point + hit.normal * .01f);
        }
        //Debug.DrawLine(base.transform.position, base.transform.TransformPoint(point));

        return point;
    }
    private Vector3 Test2DPoint(Vector3 center, Vector3 point)
    {

        Ray ray = new Ray(center, base.transform.TransformPoint(point) - center);
        if (Physics.Raycast(ray, out RaycastHit hit, maxviewDistance, enviromentLayers))
        {
            //Debug.DrawLine(center, hit.point);
            return base.transform.InverseTransformPoint(hit.point + hit.normal * .01f);
        }
        //Debug.DrawLine(center, base.transform.TransformPoint(point));

        return point;
    }
    #endregion
    private void OnValidate()
    {
        CreateLOSMesh();
        scanInterval = 1.0f / scanFrequency;

    }


    public bool CheckVisibilityTo(GameObject target)
    {
        return Physics.Linecast(base.transform.position, target.transform.position);
    }

    public void RestoreState(object state)
    {
    }
    public Vector3 GetNoiseOffset(Vector3 pestLoc)
    {
        float distanceToTarget = Vector3.Distance(base.transform.position, pestLoc);
        if (distanceToTarget < noiseMinDistance)
            return Vector3.zero;
        else
        {
            float radius = noiseRadius * distanceToTarget / maxviewDistance;

            Vector2 noise = Random.insideUnitCircle * radius;
            return new Vector3(noise.x, 0, noise.y); // should return the correct point
        }
    }



    public void DisableModule()
    {
        transform.GetComponent<MeshRenderer>().enabled = false;
        transform.GetComponent<MeshFilter>().mesh = null;
    }


    #region GL Functions
    void CreateLineMaterial()
    {
        // Unity has a built-in shader that is useful for drawing simple colored things
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        // Turn on alpha blending
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Turn backface culling off
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        // Turn off depth writes
        lineMaterial.SetInt("_ZWrite", 0);
    }
    private void MarkTargets()
    {
        if (poiTargets.Count == 0) return;

        //GL.PushMatrix();
        GL.MultMatrix(base.transform.worldToLocalMatrix);
        GL.Begin(GL.LINES);
        lineMaterial.SetPass(0);
        foreach (GameObject poi in poiTargets)
        {
            if(poi == null) continue;
            Color baseColor = Color.red;

            GL.Color(baseColor);

            GL.Vertex(base.transform.TransformPoint(base.transform.position));
            GL.Vertex(base.transform.TransformPoint(poi.transform.position));

        }

        GL.End();
        //GL.PopMatrix();
    }

    private void OnRenderObject()
    {
        if (multiLosMode) return;
        if(showTargetLines)
            MarkTargets();

    }

    #endregion

    private void OnDestroy()
    {
        if (mesh != null)
            mesh.Clear();

        poiObjects = null;
        lineMaterial = null;
    }



    public void SetDetailedScanMesh(bool active)
    {
        showDetailedScan = active;
        shouldRefreshMesh = true;
    }
    public void HandleSelect()
    {
        SetDetailedScanMesh(true);
    }

    public void HandleDeselect()
    {
        ResetMesh();

    }

    private void ResetMesh()
    {
        SetDetailedScanMesh(false);
        meshRenderer.enabled = true;
    }

    internal void ToggleMesh(bool active)
    {
        meshRenderer.enabled = active;
    }
    [ContextMenu("Detailed Scan")]
    private void ToggleMeshMode()
    {
        SetDetailedScanMesh(!showDetailedScan);
    }



    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision {collision.transform.name}");
    }
}




