using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSightParameters : ScriptableObject
{
    [Header("Prefab Root Transform")]
    [SerializeField] public GameObject root;

    [Header("What is detectable?")]
    public List<LayerDetails> detectionLayers = new();
    [SerializeField] public LayerMask enviromentLayers;

    [Header("Visual Cone Settings")]
    [Range(0, 1000)] public float maxViewDistance = 10;
    [Range(0, 1000)] public float minviewDistance = 0;
    [Range(5, 180)] public float horizontalAngle = 30;
    [Range(5, 90)] public int verticalAngle = 30;

    [SerializeField, Tooltip("Control the level of detail of the mesh")]
    [Range(1, 5)]
    public int subDivision = 3;
}
