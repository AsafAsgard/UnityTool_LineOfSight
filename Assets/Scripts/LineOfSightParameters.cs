using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LOS
{

    [CreateAssetMenu(fileName = "Parameters", menuName = "LOS/New Parameters", order = 1)]
    public class LineOfSightParameters : ScriptableObject
    {

        [SerializeField] public LayerMask targetLayers;
        [SerializeField] public LayerMask enviromentLayers;


        [Range(0, 1000)] public float maxviewDistance = 10;
        [Range(0, 1000)] public float minviewDistance = 0;

        [Range(5, 180)] public float horizontalAngle = 30;
        [Range(5, 90)] public int verticalAngle = 30;
        [Range(1, 100)] public float zoomFactor = 1;
        [Range(.5f, 1f)] public float visualError = .9f;

        //[SerializeField][Range(-1f, 1f)] public float verticalBias = 0;

        [SerializeField, Tooltip("Control the level of detail of the mesh")]
        [Range(1, 5)]
        public int subDivision = 3;

    }


}