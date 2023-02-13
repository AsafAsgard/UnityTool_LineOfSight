using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;


namespace LOS
{

    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LineOnSightBaseObject))]

    public class LineOfSightMeshManager : MonoBehaviour
    {
        [SerializeField] private MeshDrawer m_MeshDrawer;
        [SerializeField] private Material m_MeshMaterial;

        public bool enableMesh = true;

        private SerializedVector3[,] meshPoints;
        private LineOfSightParameters parameters;


        public int verticalSegments = 4;
        public int horizontalSegments = 4;


        public SerializedVector3[,] GetMeshVerticies() => meshPoints;

        private void Start()
        {
            parameters = GetComponent<LineOnSightBaseObject>().GetParameters();
            m_MeshDrawer = ScriptableObject.CreateInstance<CentricMesh3D>();
            m_MeshDrawer.Init(transform);
            InitMeshVerticiesArray();
        }

        private void Update()
        {
            CalculateMeshPoints(parameters);
            if(meshPoints!=null && m_MeshDrawer != null && enableMesh)
                m_MeshDrawer.Draw(meshPoints);
        }
        private void OnDestroy()
        {
            meshPoints = null;
        }

        private void InitMeshVerticiesArray()
        {
            verticalSegments = 4 * parameters.subDivision;
            horizontalSegments = 4 * parameters.subDivision;
            meshPoints = new SerializedVector3[verticalSegments, horizontalSegments];
        }
        #region Logic
        /// <summary>
        /// Calculate the mesh outline verticies relative to the position and rotation of the parent object
        /// </summary>
        /// <param name="parameters"></param>
        private void CalculateMeshPoints(LineOfSightParameters parameters)
        {
            if (meshPoints == null)
            {
                return;
            }

            float deltaHorizontalAngle = (parameters.horizontalAngle * 2) / horizontalSegments;
            float deltaVerticalAngle = (parameters.verticalAngle * 2) / verticalSegments;
            float currentVerticalAngle = -parameters.verticalAngle;// + verticalBias * verticalAngle;


            for (int verticalIndex = 0; verticalIndex < verticalSegments; verticalIndex++, currentVerticalAngle += deltaVerticalAngle)
            {
                float currentHorizontalAngle = -parameters.horizontalAngle;
                for (int horizontalIndex = 0; horizontalIndex < horizontalSegments; horizontalIndex++, currentHorizontalAngle += deltaHorizontalAngle)
                {
                    Vector3 direction = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.forward;
                    Vector3 point = transform.position + (transform.TransformDirection(direction.normalized) * parameters.maxviewDistance);

                    point = TestPoint(transform.position, point);
                    meshPoints[verticalIndex, horizontalIndex] = new SerializedVector3(point);
                }
            }
        }

        /// <summary>
        /// Test point for collision with enviroment layer
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        protected Vector3 TestPoint(Vector3 origin, Vector3 point)
        {
            Ray ray = new(origin, point - origin);
            if (Physics.Raycast(ray, out RaycastHit hit, parameters.maxviewDistance, parameters.enviromentLayers))
                return hit.point;

            return point;
        }
        #endregion


        private void OnDrawGizmos()
        {
            if (meshPoints == null) return;
            Gizmos.color = Color.blue;
            foreach (SerializedVector3 point in meshPoints)
            {
                
                //Gizmos.DrawLine(transform.position,point.ToVector3());
                Gizmos.DrawSphere(point.ToVector3(), .1f);
            }
        }
        
    }

}



