using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSightParameters : ScriptableObject
{
    [Range(0, 10000)] internal float viewRange;

    [Range(0, 360)] public float horizontalAngle;
    [Range(0, 360)] internal int verticalAngle;

    internal float noiseRadius;
    internal float noiseMinDistance;
    internal float visualError;
}
