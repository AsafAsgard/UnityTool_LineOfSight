using LOS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

[ExecuteAlways]
public class LineOfSightBase : MonoBehaviour
{
    [Header("Prefab Root Transform")]
    [SerializeField] private GameObject root;

    [Header("What is detectable?")]
    [SerializeField] private List<LayerDetails> detectionLayers = new();
    [SerializeField] private LayerMask enviromentLayers;

    [Header("Visual Cone Settings")]
    [SerializeField] [Range(0, 1000)] private float maxViewDistance = 10;
    [SerializeField] [Range(0, 1000)] private float minviewDistance = 0;
    [SerializeField] [Range(5, 180)] private float horizontalAngle = 30;
    [SerializeField] [Range(5, 90)] private int verticalAngle = 30;
    [SerializeField, Tooltip("Control the level of detail of the mesh")]
    [Range(1, 5)]
    private int subDivision = 3;

    //[Range(.5f, 1f)] private float visualError = .9f;
    //[Range(0, 150)] public float DroneDetectionRadius = 50;
    //[Range(0, 50)] public float DroneDetectionHeight = 20;

    //[Range(1, 100)] public float zoomFactor = 1;

    //[SerializeField][Range(-1f, 1f)] public float verticalBias = 0;

    [Header("Shared Paramenters")]
    public LineOfSightParameters parameters;


    private List<ISightModule> modules = new();

    private void Awake()
    {
        parameters = ScriptableObject.CreateInstance<LineOfSightParameters>();
        parameters.root = gameObject;
    }

    

    public void SetTagDetectionDistance(string targetTag, float distance)
    {
        LayerDetails layer = detectionLayers.SingleOrDefault(dl => dl.targetTag.Equals(targetTag));
        if(layer == null)
            detectionLayers.Add(new LayerDetails(targetTag, distance));
        else
            layer.distance = distance;
        UpdateViewDistance();
    }

    private void UpdateViewDistance()
    {
        maxViewDistance = detectionLayers.Select(dl => dl.distance).ToList().Max();
    }


    #region Modules
    private void OnValidate()
    {
        parameters.root = root;
        parameters.minviewDistance = minviewDistance;
        parameters.maxViewDistance = maxViewDistance;
        parameters.detectionLayers = detectionLayers;
        parameters.enviromentLayers = enviromentLayers;
        parameters.horizontalAngle = horizontalAngle;
        parameters.verticalAngle = verticalAngle;
        parameters.subDivision = subDivision;
    }

    private void UpdateModules()
    {
        foreach (ISightModule module in modules)
        {
            module.UpdateParameters();
        }
    }
    internal void Register(ISightModule module)
    {
        if(modules.Contains(module)) return;
        modules.Add(module);
    }

    #endregion
}
[Serializable]
public class LayerDetails
{
    public string targetTag;
    [Range(0, 1000)] public float distance;
    public LayerDetails(string targetTag, float distance)
    {
        this.targetTag = targetTag;
        this.distance = distance;
    }

}