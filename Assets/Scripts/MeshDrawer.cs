using JetBrains.Annotations;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public abstract class MeshDrawer : ScriptableObject{

    protected Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    Transform refTransform;
    [SerializeField][Range(-1f, 1f)] public float verticalBias;
    [Range(1, 5)]
    public int subDivision = 3;
    public int verticalSegments = 4;
    public int horizontalSegments = 4;
    public bool showDetailedScan = false;


    public void Init(Transform losTransform)
    {
        refTransform = losTransform;
        mesh = new Mesh();
        mesh.name = "LOS";

        meshFilter = refTransform.GetComponent<MeshFilter>();
        meshRenderer = refTransform.GetComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
        meshRenderer.enabled = true;

    }
    private void OnEnable()
    {

        
        verticalSegments = 4 * subDivision;
        horizontalSegments = 4 * subDivision;
    }
    public virtual void Draw(Transform refTransform, LineOfSightParameters parameters) { }


    protected Vector3 TestPoint(Vector3 origin, Vector3 point, float range, LayerMask blockingLayers)
    {
        Ray ray = new Ray(origin, point - origin);
        if (Physics.Raycast(ray, out RaycastHit hit, range, blockingLayers))
            return hit.point;
        
        return point;
    }
}
