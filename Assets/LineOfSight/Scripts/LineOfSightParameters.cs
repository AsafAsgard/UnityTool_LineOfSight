using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineOfSightParameters : ScriptableObject
{
    [Header("Prefab Root Transform")]
    [SerializeField] public GameObject root;

    [Header("What is detectable?")]
    public Dictionary<string, float> tagsDictionary = new();
    [SerializeField] public LayerMask enviromentLayers;

    [Header("Visual Cone Settings")]
    [Range(0, 1000)] public float maxViewDistance = 10;
    [Range(0, 360)] public float horizontalAngle = 50;
    [Range(0, 180)] public float verticalAngle = 30;

    [SerializeField, Tooltip("Control the level of detail of the mesh")]
    [Range(1, 10)]
    public int subDivision = 3;
}
