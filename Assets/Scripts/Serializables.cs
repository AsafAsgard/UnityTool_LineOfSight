using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LOS
{
    public class Serializables : MonoBehaviour
    {
    }

    [Serializable]
    public class DetectionTypeDistance
    {
        public LayerMask layer;
        [Range(0, 1000)] public float distance;
    }


    [Serializable]
    public struct POI
    {
        public string identifier;
        public Vector3 point;
    }

    /// <summary>
    /// Serialize and Unserialize Vector3
    /// </summary>
    [System.Serializable]
    public struct SerializedVector3
    {
        public float x; public float y; public float z;

        public SerializedVector3(Vector3 vector)
        {
            x = vector.x; y = vector.y; z = vector.z;
        }
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}