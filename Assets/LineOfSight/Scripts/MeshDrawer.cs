using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LOS
{
    [ExecuteAlways]
    public abstract class MeshDrawer : ScriptableObject
    {
        public bool Initialized { get; private set; } = false;

        [SerializeField] private Material useMaterial;
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
            meshRenderer.material = useMaterial;

            Initialized = true;
        }

        public abstract void Draw(in Vector3[,] meshPoints);


        private void OnDestroy()
        {
            mesh = null;
        }
    }

}