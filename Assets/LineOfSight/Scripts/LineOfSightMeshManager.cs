using System;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace LOS
{
    [ExecuteAlways]

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LineOfSightBase))]

    public class LineOfSightMeshManager : MonoBehaviour, ISightModule
    {
        public Vector3[,] MeshPoints { get; private set; }
        [SerializeField] private MeshDrawer meshDrawer;
        private int segmentResolution = 4;
        private float deltaHorizontalAngle;
        private float deltaVerticalAngle;


        private NativeArray<RaycastCommand> _raycastCommands;
        private NativeArray<RaycastHit> _raycastHits;
        private JobHandle _jobHandle;
        private QueryParameters queryParameters;


        private LineOfSightParameters _parameters;

        private void Start()
        {
            AssignDrawer();
        }
        private void OnDestroy()
        {
            MeshPoints = null;

            _jobHandle.Complete();
            if (_raycastCommands.IsCreated)
                _raycastCommands.Dispose();
            if (_raycastHits.IsCreated)
                _raycastHits.Dispose();
        }

        [ContextMenu("Init")]
        public void Initialize(LineOfSightParameters parameters)
        {
            _parameters = parameters;
            segmentResolution = 4 * _parameters.subDivision;

            if (_raycastCommands.IsCreated && _raycastCommands != null)
                _raycastCommands.Dispose();
            if (_raycastHits.IsCreated && _raycastHits != null)
                _raycastHits.Dispose();

            _raycastCommands = new NativeArray<RaycastCommand>((segmentResolution + 1) * (segmentResolution + 1), Allocator.Persistent);
            _raycastHits = new NativeArray<RaycastHit>((segmentResolution + 1) * (segmentResolution + 1), Allocator.Persistent);
            if (_parameters.horizontalAngle == 0 || _parameters.verticalAngle == 0)
                MeshPoints = null;
            else
                MeshPoints = new Vector3[segmentResolution + 1, segmentResolution + 1];
        }


        public void AssignDrawer()
        {
            meshDrawer = Resources.Load<CentricMesh3D>("Drawers/Centric Mesh 3D");
            if (meshDrawer != null)
            {
                meshDrawer.Init(transform);
            }
        }
        private void Update()
        {
            //InitMeshData();

            UpdateMatrix();
        }

        public void UpdateMatrix()
        {
            if (_parameters == null) return;
            CalculateMeshPoints();
        }

        private void DrawMesh()
        {
            if (MeshPoints == null || meshDrawer == null) return;
            if (!meshDrawer.Initialized)
                meshDrawer.Init(transform);
            meshDrawer.Draw(MeshPoints);
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
            deltaHorizontalAngle = _parameters.horizontalAngle / segmentResolution;
            deltaVerticalAngle = _parameters.verticalAngle / segmentResolution;
            queryParameters.layerMask = _parameters.enviromentLayers;

            float currentVerticalAngle = -_parameters.verticalAngle / 2;
            for (int verticalIndex = 0; verticalIndex <= segmentResolution; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
            {
                float currentHorizontalAngle = -_parameters.horizontalAngle / 2;
                for (int horizontalIndex = 0; horizontalIndex <= segmentResolution; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
                {
                    Vector3 direction = (Quaternion.AngleAxis(currentHorizontalAngle, transform.up) * Quaternion.AngleAxis(currentVerticalAngle, transform.right)) * transform.forward;

                    Debug.DrawRay(transform.position, direction);

                    RaycastCommand raycast = new(
                        from: transform.position,
                        direction: direction,
                        queryParameters,
                        distance: _parameters.maxViewDistance
                        );

                    _raycastCommands[verticalIndex * segmentResolution + horizontalIndex + verticalIndex] = raycast;

                    Vector3 defaultPoint = transform.position + direction.normalized * _parameters.maxViewDistance;
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

            for (int row = 0; row <= segmentResolution; row++)
            {
                for (int col = 0; col <= segmentResolution; col++)
                {
                    RaycastHit hit = _raycastHits[row * segmentResolution + col + row];
                    if (hit.transform == null) continue;
                    MeshPoints[row, col] = hit.point + hit.normal * 0.01f;
                }
            }
        }
        #endregion


        private void OnDrawGizmos()
        {
            if (MeshPoints == null || _parameters == null) return;
            foreach (Vector3 point in MeshPoints)
            {
                float distance = Vector3.Distance(transform.position, point);
                Gizmos.color = distance < _parameters.maxViewDistance - .01f ? Color.yellow : Color.blue;

                Gizmos.DrawSphere(point, .1f);
            }
        }


    }

}



