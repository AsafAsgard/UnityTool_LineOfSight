using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using Random = UnityEngine.Random;

namespace LOS
{
    [ExecuteAlways]
    public class LineOnSightBaseObject : MonoBehaviour
    {
        public string FieldName { get; } = "LOS";

        [Header("Appearance")]
        [SerializeField] public bool centric = true;

        [SerializeField] private LineOfSightParameters parameters;
        public LineOfSightParameters GetParameters() { return parameters; }

        [Header("Scan Options")]
        [SerializeField] private float scanInterval = 0.02f;
        private float timeSinceLastScan = Mathf.Infinity;


        [Header("Noise Options")]
        [SerializeField] private float noiseRadius = 0;
        [SerializeField] private float noiseMinDistance = 0;
        //[SerializeField] private float errorChance = 0f;

        [Header("Debug")]
        //[SerializeField] public bool enable2dMesh = false;
        [SerializeField] public bool showTargetLines = false;

        //[SerializeField] LineOfSightParameters lineOfSightParameters;

        /// <summary>
        /// Debug GL line material
        /// </summary>
        private Material lineMaterial;

        /// <summary>
        /// All relevant objects in cone of sight
        /// </summary>
        private List<GameObject> poiTargets;


        private bool isScanning = false;



        public List<GameObject> GetTargets() => poiTargets;

        private void Awake()
        {
            isScanning = false;
            poiTargets = new List<GameObject>();
            CreateLineMaterial();
        }


        void Update()
        {
            timeSinceLastScan += Time.deltaTime;
            if (isScanning || timeSinceLastScan < scanInterval)
            {
                timeSinceLastScan = 0;
                Scan();
            }
        }

        #region Scan

        /// <summary>
        /// Check all objects within a given radius and a Target layer mask;
        /// Save to poiTargets only the targets that are visible
        /// </summary>
        [ContextMenu("Scan")]
        private void Scan()
        {
            isScanning = true;

            //Get targets in range
            GameObject[] pois = Physics.OverlapSphere(transform.position, parameters.maxviewDistance, parameters.targetLayers)
                                        .Select(o => o.gameObject).ToArray();
            poiTargets.Clear();
            poiTargets = pois.Where(poi => TargetInSight(poi.transform.position)).ToList();
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
        private bool TargetInSight(Vector3 targetPos)
        {
            //Can be seen
            if (Physics.Linecast(transform.position, targetPos, parameters.enviromentLayers)) return false;

            //Validate angles
            Quaternion angleToTarget = Quaternion.FromToRotation(transform.forward, targetPos - transform.position);

            float targetHorizontalAngle = angleToTarget.eulerAngles.y;
            if (targetHorizontalAngle > 180)
                targetHorizontalAngle -= 360;
            float targetVerticalAngle = angleToTarget.eulerAngles.x;
            if (targetVerticalAngle > 180)
                targetVerticalAngle -= 360;
            if (Mathf.Abs(targetHorizontalAngle) > parameters.horizontalAngle) return false;
            if (Mathf.Abs(targetVerticalAngle) > parameters.verticalAngle) return false;

            return true;

        }


        public Vector3 GetNoiseOffset(Vector3 pestLoc)
        {
            float distanceToTarget = Vector3.Distance(transform.position, pestLoc);
            if (distanceToTarget < noiseMinDistance)
                return Vector3.zero;
            else
            {
                float radius = noiseRadius * distanceToTarget / parameters.maxviewDistance;

                Vector2 noise = Random.insideUnitCircle * radius;
                return new Vector3(noise.x, 0, noise.y); // should return the correct point
            }
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


