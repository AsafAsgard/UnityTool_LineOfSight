using JetBrains.Annotations;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public abstract class MeshDrawer : ScriptableObject{

    protected Mesh mesh;
    [SerializeField][Range(-1f, 1f)] public float verticalBias;
    [Range(1, 5)]
    public int subDivision = 3;
    public int verticalSegments = 4;
    public int horizontalSegments = 4;
    public bool showDetailedScan = false;


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
