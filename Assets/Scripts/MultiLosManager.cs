
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[ExecuteAlways]
public class MultiLosManager : MonoBehaviour
{

    private Material lineMaterial;


    [Header("Inner LOS")]
    [SerializeField] private LOSModule innerLOS;
    [SerializeField] private Material innerMaterial;

    [Header("Outter LOS")]
    [SerializeField] private LOSModule outterLOS;
    [SerializeField] private Material outterMaterial;

    [Header("General")]
    [SerializeField] private float maxviewDistance = 5;

    internal void ToggleMeshMode()
    {
        enable2D = !enable2D;
        innerLOS.enable2dMesh = enable2D;
        outterLOS.enable2dMesh = enable2D;
    }

    [SerializeField] private float minviewDistance = 0;
    [SerializeField] private bool centric = false;
    [SerializeField] private bool showDetailedScan = false;
    [SerializeField] private bool enable2D= false;

    [SerializeField][Range(0f, 1f)] private float distanceBias = 0.5f;
    [SerializeField][Range(-1f, 1f)] private float verticalBias;
    [SerializeField][Range(0, 180)] private float horizontalAngle = 1;
    [SerializeField][Range(0, 90)] private float verticalAngle = 1;
    [SerializeField][Range(1, 10)] private int subDivision = 2;

    private List<GameObject> innerTargets;
    private List<GameObject> outterTargets;
    void Start()
    {
        if (innerLOS == null || outterLOS == null) return;
        innerLOS.transform.position = transform.position;
        outterLOS.transform.position = transform.position;
        innerLOS.SetMultiLos();
        outterLOS.SetMultiLos();
        CreateLineMaterial();
    }

    private void OnValidate()
    {
        if (innerLOS == null || outterLOS == null) return;
        innerLOS.horizontalAngle = horizontalAngle;
        innerLOS.verticalAngle = verticalAngle;
        innerLOS.subDivision = subDivision;
        innerLOS.centric = centric;
        innerLOS.verticalBias = verticalBias;
        //innerLOS.meshMaterial = innerMaterial;
        innerLOS.showDetailedScan = showDetailedScan;
        innerLOS.enable2dMesh = enable2D;

        outterLOS.horizontalAngle = horizontalAngle;
        outterLOS.verticalAngle = verticalAngle;
        outterLOS.subDivision = subDivision;
        outterLOS.centric = centric;
        outterLOS.verticalBias = verticalBias;
        //outterLOS.meshMaterial = outterMaterial;
        outterLOS.showDetailedScan = showDetailedScan;
        outterLOS.enable2dMesh = enable2D;

        innerLOS.minviewDistance = minviewDistance;
        outterLOS.maxviewDistance = maxviewDistance;


        innerLOS.maxviewDistance = minviewDistance + ((maxviewDistance - minviewDistance) * distanceBias); 

        outterLOS.minviewDistance = innerLOS.maxviewDistance;

        CreateLineMaterial();

        innerLOS.SetMultiLos();
        outterLOS.SetMultiLos();
        innerLOS.RefreshMesh();
        outterLOS.RefreshMesh();

    }
    
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

    private void Update()
    {
        //innerTargets = innerLOS.GetTargetList();
        //foreach(Targetable target in innerTargets.Select(o=>o.GetComponent<Targetable>()))
        //{
        //    target.isKnown = true;
        //    target.ShowSign();
        //}
        //outterTargets = outterLOS.GetTargetList();

        //outterTargets = outterTargets.Where(target => !innerTargets.Contains(target)).ToList();
        //foreach (Targetable target in outterTargets.Select(o => o.GetComponent<Targetable>()))
        //{
        //    target.ShowSign();
        //}
    }

    public List<GameObject> GetTargets()
    {
        if (innerTargets == null || outterTargets == null) return null;
        List<GameObject> targets = new List<GameObject>();
        targets.AddRange(innerTargets);
        targets.AddRange(outterTargets);
        return targets;
    }
    //private void MarkTargets(List<GameObject> targetList, Color color)
    //{
    //    if (targetList == null || targetList.Count == 0) return;

    //    //GL.PushMatrix();
    //    GL.MultMatrix(base.transform.worldToLocalMatrix);
    //    GL.Begin(GL.LINES);
    //    lineMaterial.SetPass(0);
    //    foreach (GameObject poi in targetList)
    //    {
    //        Color baseColor = color;

    //        GL.Color(baseColor);

    //        GL.Vertex(base.transform.TransformPoint(base.transform.position));
    //        GL.Vertex(base.transform.TransformPoint(poi.transform.position));

    //    }

    //    GL.End();
    //    //GL.PopMatrix();
    //}

    //private void OnRenderObject()
    //{
    //    MarkTargets(innerTargets,Color.yellow);
    //    MarkTargets(outterTargets,Color.red);
    //}

}
