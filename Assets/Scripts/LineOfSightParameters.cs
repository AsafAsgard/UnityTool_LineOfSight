using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Parameters",menuName = "LOS/New Parameters",order = 1)]
public class LineOfSightParameters : ScriptableObject
{
    public DetectionTypeDistance enviromentDetection;
    public DetectionTypeDistance[] entityDetectionRanges;

    [Range(5, 180)] public float horizontalAngle = 30;
    [Range(5, 180)] public int verticalAngle = 30;
    [Range(1, 100)] public float zoomFactor = 1;

    [Range(.5f, 1f)] public float visualError = .9f;
}

[Serializable]
public class DetectionTypeDistance
{
    public LayerMask layer;
    [Range(0, 1000)] public float distance;
}
