﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.VisualScripting;
using UnityEngine;


using Random = UnityEngine.Random;
namespace LOS
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineOfSightBase))]
    public class LineOfSightScanner : MonoBehaviour, ISightModule
    {
        public enum ScanErrorType { Fixed, Curve }
        public string FieldName { get; } = "LOS";

        private LineOfSightParameters _parameters;

        [Header("Scan Options")]
        [SerializeField] private float scanInterval = 0.5f;
        private float timeSinceLastScan = Mathf.Infinity;


        [Header("Noise Options")]
        [SerializeField] private bool enableNoise;
        [SerializeField] private float noiseMaxRadius = 0;
        [SerializeField] private float noiseMinDistance = 0;
        [SerializeField] private float noiseMaxDistance = 0;

        [Header("Identification Error")]
        [SerializeField] private bool enableIdError;
        [SerializeField] private float errorProbability = 0;

        [Header("Debug")]
        [SerializeField, Tooltip("There lines will be visible in Game Mode!")]
        public bool showTargetLines = true;


        private static List<GameObject> allEntities;
        private bool isScanning = false;

        /// <summary>
        /// Debug GL line material
        /// </summary>
        private Material lineMaterial;

        /// <summary>
        /// All relevant objects in cone of sight
        /// </summary>
        private List<GameObject> poiTargets;

        public List<T> GetTargets<T>(Func<GameObject, T> converter)
        {
            return poiTargets
                .Select(t => converter(t))
                .ToList();
        }
        
       

        public List<GameObject> GetTargets() => poiTargets;

        private void Awake()
        {
            isScanning = false;
            poiTargets = new();
            CreateLineMaterial();
        }


        [ContextMenu("Init")]
        public void Initialize(LineOfSightParameters parameters)
        {
            _parameters = parameters;
            allEntities = new();
            if (_parameters.tagsDictionary == null || _parameters.tagsDictionary.Count == 0) return;
            foreach (string tag in parameters.tagsDictionary.Keys)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                GameObject[] items = GameObject.FindGameObjectsWithTag(tag);
                if (items != null)
                    allEntities.AddRange(items);
            }
        }


        void Update()
        {
            timeSinceLastScan += Time.deltaTime;
            if (isScanning || timeSinceLastScan < scanInterval) return;

            timeSinceLastScan = 0;
            Scan();

        }

        #region Scan

        /// <summary>
        /// Check all objects within a given radius and a Target layer mask;
        /// Save to poiTargets only the targets that are visible
        /// </summary>
        [ContextMenu("Scan")]
        [BurstCompile]
        private void Scan()
        {
            isScanning = true;
            poiTargets.Clear();
            //Add targets by tag
            GameObject entity;
            if (allEntities != null && _parameters.tagsDictionary != null)
            {
                for (int i = 0; i < allEntities.Count; i++)
                {
                    entity = allEntities[i];

                    if (entity == _parameters.root) continue; // prevent seld id
                    if (!_parameters.tagsDictionary.Keys.Contains(entity.tag)) continue; // ID-able tag
                    if (Vector3.Distance(transform.position, entity.transform.position) > _parameters.tagsDictionary[entity.tag]) continue;

                    if (TargetInSight(entity.transform))
                        poiTargets.Add(entity);
                }
            }
            isScanning = false;
        }


        /// <summary>
        /// Checks if a point is within the LOS angle and is not blocked by enviroment object (check by layer)
        /// </summary>
        /// <remarks>
        /// NOTE: The point is already in range
        /// </remarks>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        private bool TargetInSight(Transform target)
        {
            Vector3 targetPoistion;
            if (transform.TryGetComponent(out Collider col))
            {
                targetPoistion = col.bounds.center;
            }
            else
            {
                targetPoistion = target.position;
            }

            //Can be seen
            if (Physics.Linecast(transform.position, targetPoistion, _parameters.enviromentLayers)) return false;

            //Validate angles
            Quaternion angleToTarget = Quaternion.FromToRotation(transform.forward, targetPoistion - transform.position);

            if (_parameters.horizontalAngle < 180)
            {
                float targetHorizontalAngle = angleToTarget.eulerAngles.y;
                if (targetHorizontalAngle > 180)
                    targetHorizontalAngle -= 360;
                if (Mathf.Abs(targetHorizontalAngle) > _parameters.horizontalAngle / 2) return false;

            }
            if (_parameters.verticalAngle < 90)
            {
                float targetVerticalAngle = angleToTarget.eulerAngles.x;
                if (targetVerticalAngle > 180)
                    targetVerticalAngle -= 360;
                if (Mathf.Abs(targetVerticalAngle) > _parameters.verticalAngle / 2) return false;
            }
            return true;

        }


        public Vector3 GetNoiseOffset(Vector3 pestLoc)
        {
            if (!enableNoise)
                return Vector3.zero;
            float distanceToTarget = Vector3.Distance(transform.position, pestLoc);
            if (distanceToTarget > noiseMinDistance)
            {
                float noiseRadiusModifier = Mathf.Clamp01(distanceToTarget / noiseMaxDistance);
                
                Vector2 noise = noiseMaxRadius * noiseRadiusModifier * Random.insideUnitCircle;
                return new Vector3(noise.x, 0, noise.y); // should return the correct point
            }
            return Vector3.zero;
        }

        #endregion

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
            if (showTargetLines)
                MarkTargets();
        }

        #endregion

        private void OnDestroy()
        {
            poiTargets = null;
            lineMaterial = null;
        }

    }
}

public struct TargetObject
{
    public string name;
    public Vector3 position;
    public bool alive;
}

