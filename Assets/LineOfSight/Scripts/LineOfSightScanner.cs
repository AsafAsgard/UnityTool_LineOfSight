using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;


using Random = UnityEngine.Random;
namespace LOS
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineOfSightBase))]
    public class LineOnSightScanner : MonoBehaviour, ISightModule
    {
        public string FieldName { get; } = "LOS";

        [Header("Scan Options")]
        [SerializeField] private float scanInterval = 0.5f;
        private float timeSinceLastScan = Mathf.Infinity;


        [Header("Noise Options")]
        [SerializeField] private float noiseRadius = 0;
        [SerializeField] private float noiseMinDistance = 0;
        //[SerializeField] private float errorChance = 0f;

        [Header("Debug")]
        [SerializeField , Tooltip("There lines will be visible in Game Mode!")] 
        public bool showTargetLines = true;



        private static List<GameObject> allEntities;
        private LineOfSightParameters parameters;
        private bool isScanning = false;

        /// <summary>
        /// Debug GL line material
        /// </summary>
        private Material lineMaterial;

        /// <summary>
        /// All relevant objects in cone of sight
        /// </summary>
        private List<GameObject> poiTargets;

        private LineOfSightBase lineOfSightBase;


        public List<GameObject> GetTargets() => poiTargets;

        private void Awake()
        {
            lineOfSightBase = GetComponent<LineOfSightBase>();
            lineOfSightBase.Register(this);
            isScanning = false;
            poiTargets = new();
            CreateLineMaterial();
        }

        private void Start()
        {
            Initialize();
        }

        [ContextMenu("Init")]
        public void Initialize()
        {
            if (lineOfSightBase == null)
                lineOfSightBase = GetComponent<LineOfSightBase>();
            parameters = lineOfSightBase.parameters;
            allEntities = new();
            if (parameters.tagsDictionary == null || parameters.tagsDictionary.Count == 0) return;
            foreach (string tag in parameters.tagsDictionary.Keys)
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag(tag);
                if (items != null)
                    allEntities.AddRange(items); 
            }
        }


        void Update()
        {
            if (parameters == null) return;
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
            if(allEntities != null && parameters.tagsDictionary != null)
            {
                for (int i = 0; i < allEntities.Count; i++)
                {
                    GameObject entity = allEntities[i];

                    if (entity == parameters.root) continue;
                    if (!parameters.tagsDictionary.Keys.Contains(entity.tag)) continue;
                    if (Vector3.Distance(transform.position, entity.transform.position) > parameters.tagsDictionary[entity.tag]) continue;
                    if (TargetInSight(entity.transform))
                        poiTargets.Add(entity);
                }
                //foreach (LayerDetails layerDetail in parameters.detectionLayers)
                //{
                //    IEnumerable<GameObject> targetsInLayer = allEntities.Where(target => target.tag.Equals(layerDetail.targetTag));
                //    IEnumerable<GameObject> targetsInRange = targetsInLayer.Where(entity => Vector3.Distance(transform.position, entity.transform.position) <= layerDetail.distance);
                //    IEnumerable<GameObject> layerPOI = targetsInRange.Where(poi => TargetInSight(poi.transform));
                //    poiTargets.AddRange(layerPOI);
                //}
            }

            //if (poiTargets.Contains(parameters.root))
            //    poiTargets.Remove(parameters.root);
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
            if (Physics.Linecast(transform.position, targetPoistion, parameters.enviromentLayers)) return false;

            //Validate angles
            Quaternion angleToTarget = Quaternion.FromToRotation(transform.forward, targetPoistion - transform.position);

            if(parameters.horizontalAngle < 180)
            {
                float targetHorizontalAngle = angleToTarget.eulerAngles.y;
                if (targetHorizontalAngle > 180)
                    targetHorizontalAngle -= 360;
                if (Mathf.Abs(targetHorizontalAngle) > parameters.horizontalAngle/2) return false;

            }
            if(parameters.verticalAngle < 90)
            {
                float targetVerticalAngle = angleToTarget.eulerAngles.x;
                if (targetVerticalAngle > 180)
                    targetVerticalAngle -= 360;
                if (Mathf.Abs(targetVerticalAngle) > parameters.verticalAngle/2) return false;
            }
            return true;

        }


        public Vector3 GetNoiseOffset(Vector3 pestLoc)
        {
            float distanceToTarget = Vector3.Distance(transform.position, pestLoc);
            if (distanceToTarget < noiseMinDistance)
                return Vector3.zero;
            else
            {
                float radius = noiseRadius * distanceToTarget / parameters.maxViewDistance;

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


