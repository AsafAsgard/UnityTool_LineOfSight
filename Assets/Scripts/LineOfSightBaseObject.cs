using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LineOnSightBaseObject : MonoBehaviour
{
    public string FieldName { get; } = "LOS";
    private MeshDrawer mDrawer;

    [Header("Appearance")]
    [SerializeField] public bool centric = true;
    public bool showDetailedScan = false;
    [SerializeField, Tooltip("Control the level of detail of the mesh")]
    [Range(1, 10)]
    public int subDivision = 3;


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


    private Material lineMaterial;

    private static List<GameObject> poiObjects;

    private List<GameObject> poiTargets;
    private bool isScanning = false;
    private bool multiLosMode = false;

    private float scanInterval;
    private float scanTimer;



    public List<GameObject> GetTargets() => poiTargets;

    private void Awake()
    {
 
        mDrawer = ScriptableObject.CreateInstance<CentricMesh3D>();
        mDrawer.Init(transform);

        isScanning = false;

        poiTargets = new List<GameObject>();

        CreateLineMaterial();


    }

    public void SetMultiLos()
    {
        multiLosMode = true;
    }
    void Start()
    {
        SetDetailedScanMesh(showDetailedScan);
        scanInterval = 1.0f / scanFrequency;
    }

    void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0 && !isScanning)
        {
            scanTimer += scanInterval;
            CreateLOSMesh();
            Scan();
        }

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
        LineOfSightParameters parameters = ScriptableObject.CreateInstance<LineOfSightParameters>();
        mDrawer.Draw(transform,parameters);

    }

    private void OnValidate()
    {
        //CreateLOSMesh();
        scanInterval = 1.0f / scanFrequency;

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

    #endregion

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
            if (poi == null) continue;
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
        if (showTargetLines)
            MarkTargets();

    }

    #endregion

    private void OnDestroy()
    {
        poiObjects = null;
        lineMaterial = null;
    }



    public void SetDetailedScanMesh(bool active)
    {
        showDetailedScan = active;
    }
    public void HandleSelect()
    {
        SetDetailedScanMesh(true);
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

[Serializable]
public struct POI
{
    public string identifier;
    public Vector3 point;
}

