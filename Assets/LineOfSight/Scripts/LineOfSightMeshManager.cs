using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace LOS
{
    [ExecuteAlways]

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LineOfSightBase))]

    public class LineOfSightMeshManager : MonoBehaviour
    {
        public Vector3[,] MeshPoints { get; private set; }
        [SerializeField] private MeshDrawer meshDrawer;
        private LineOfSightParameters parameters;
        private int segmentResolution = 4;

        private float deltaHorizontalAngle;
        private float deltaVerticalAngle;


        private NativeArray<RaycastCommand> _raycastCommands;
        private NativeArray<RaycastHit> _raycastHits;
        private JobHandle _jobHandle;
        QueryParameters queryParameters;

        private void Awake()
        {
            parameters = GetComponent<LineOfSightBase>().parameters;
        }
        private void Start()
        {
            if (parameters != null)
            {
                InitMeshData();
            }
        }
        private void OnDestroy()
        {
            MeshPoints = null;

            _jobHandle.Complete();
            _raycastCommands.Dispose();
            _raycastHits.Dispose();
        }


        [ContextMenu("Init")]
        public void UpdateParameters()
        {
            InitMeshData();
        }
        private void InitMeshData()
        {
            meshDrawer?.Init(transform);
            InitMeshVerticiesArray();
            _raycastCommands = new NativeArray<RaycastCommand>(segmentResolution * segmentResolution, Allocator.Persistent);
            _raycastHits = new NativeArray<RaycastHit>(segmentResolution * segmentResolution, Allocator.Persistent);

            deltaHorizontalAngle = (parameters.horizontalAngle * 2) / segmentResolution;
            deltaVerticalAngle = (parameters.verticalAngle * 2) / segmentResolution;
            queryParameters.layerMask = parameters.enviromentLayers;

        }

        private void Update()
        {
            InitMeshData();
            UpdateMatrix();
        }

        public void UpdateMatrix()
        {
            if (parameters == null) return;
            CalculateMeshPoints();
        }

        private void DrawMesh()
        {
            if (MeshPoints == null) return;
            if (meshDrawer == null) return;
            if (!meshDrawer.Initialized)
                meshDrawer.Init(transform);
            meshDrawer.Draw(MeshPoints);
        }

        private void InitMeshVerticiesArray()
        {
            segmentResolution = 4 * parameters.subDivision;
            MeshPoints = new Vector3[segmentResolution, segmentResolution];
        }
        #region Logic
        /// <summary>
        /// Calculate the mesh outline verticies relative to the position and rotation of the parent object
        /// </summary>
        /// <param name="parameters"></param>
        private void CalculateMeshPoints()
        {
            if (MeshPoints == null || !_jobHandle.IsCompleted)
            {
                _jobHandle.Complete();
                return;
            }

            float currentVerticalAngle = -parameters.verticalAngle;

            for (int verticalIndex = 0; verticalIndex < segmentResolution; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
            {
                float currentHorizontalAngle = -parameters.horizontalAngle;
                for (int horizontalIndex = 0; horizontalIndex < segmentResolution; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
                {
                    Vector3 direction = (Quaternion.AngleAxis(currentHorizontalAngle, transform.up) * Quaternion.AngleAxis(currentVerticalAngle, transform.right)) * transform.forward;

                    //Debug.DrawRay(transform.position, direction);

                    RaycastCommand raycast = new(
                        from: transform.position,
                        direction: direction,
                        queryParameters,
                        distance: parameters.maxViewDistance
                        );

                    _raycastCommands[verticalIndex * segmentResolution + horizontalIndex] = raycast;

                    Vector3 defaultPoint = transform.position + direction.normalized * parameters.maxViewDistance;
                    MeshPoints[verticalIndex, horizontalIndex] = defaultPoint;
                }
            }
            _jobHandle = RaycastCommand.ScheduleBatch(_raycastCommands, _raycastHits, segmentResolution);
        }

        private void LateUpdate()
        {
            HandleRaycastResults();

            DrawMesh();

        }

        private void HandleRaycastResults()
        {
_jobHandle.Complete();
            if (!_raycastHits.IsCreated) return;

            for (int v = 0; v < segmentResolution; v++)
            {
                for (int h = 0; h < segmentResolution; h++)
                {
                    RaycastHit hit = _raycastHits[v * segmentResolution + h];
                    if (hit.transform == null) continue;
                    MeshPoints[v, h] = hit.point;
                }
            }        }
        #endregion


        private void OnDrawGizmos()
        {
            if (MeshPoints == null) return;
            foreach (Vector3 point in MeshPoints)
            {
                float distance = Vector3.Distance(transform.position, point);
                Gizmos.color = distance < parameters.maxViewDistance - .01f ? Color.yellow : Color.blue;

                Gizmos.DrawSphere(point, .1f);
            }
        }

    }

}



