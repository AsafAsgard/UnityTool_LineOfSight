using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LOS
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class MeshDrawer : ScriptableObject
    {

        protected Mesh mesh;
        protected MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        protected Transform refTransform;
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
        public virtual void Draw(SerializedVector3[,] meshPoints) { }


        public virtual void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3) { }
        public virtual void DrawQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {

            DrawTriangle(p1, p2, p3);
            DrawTriangle(p4, p2, p3);
        }

    }
}