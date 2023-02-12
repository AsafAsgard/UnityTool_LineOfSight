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

        public virtual void Draw(SerializedVector3[,] meshPoints) { }


        private void OnDestroy()
        {
            mesh = null;
        }
    }
}