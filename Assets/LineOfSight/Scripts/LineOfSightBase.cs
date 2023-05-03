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
    [SerializeField] private List<LayerDetails> detectionTags = new();
    [SerializeField] private LayerMask enviromentLayers;
    private Dictionary<string, float> tagsDictionary = new();

    [Header("Visual Cone Settings")]
    [SerializeField] [Range(0, 1000)] private float maxViewDistance = 10;
    [SerializeField] [Range(5, 180)] private float horizontalAngle = 60;
    [SerializeField] [Range(5, 90)] private int verticalAngle = 30;
    [SerializeField, Tooltip("Controls the level of detail of the mesh")]
    [Range(1, 10)]
    private int subDivision = 3;

    private List<ISightModule> modules = new();

    [Header("Shared Paramenters")]
    [Tooltip("Set a parameters object here to have it shared with other LOS objects.\nLeave empty to generate a new parameters object for this instance.")]
    public LineOfSightParameters parameters;
    private void Awake()
    {
        CreateLayerDictionary();

        parameters = ScriptableObject.CreateInstance<LineOfSightParameters>();
        parameters.root = gameObject;
        parameters.tagsDictionary = tagsDictionary;
    }

    private void CreateLayerDictionary()
    {
        tagsDictionary.Clear();
        foreach (LayerDetails layer in detectionTags)
        {
            if(layer == null) continue;
            tagsDictionary.Add(layer.targetTag, layer.distance);
        }
    }


    public void SetTagDetectionDistance(string targetTag, float distance)
    {
        LayerDetails layer = detectionTags.SingleOrDefault(dl => dl.targetTag.Equals(targetTag));
        if(layer == null)
            detectionTags.Add(new LayerDetails(targetTag, distance));
        else
            layer.distance = distance;
        UpdateViewDistance();
    }

    private void UpdateViewDistance()
    {
        maxViewDistance = detectionTags.Select(dl => dl.distance).ToList().Max();
    }


    private void OnValidate()
    {
        CreateLayerDictionary();
        parameters.root = root;
        parameters.maxViewDistance = maxViewDistance;
        parameters.tagsDictionary = tagsDictionary;
        parameters.enviromentLayers = enviromentLayers;
        parameters.horizontalAngle = horizontalAngle;
        parameters.verticalAngle = verticalAngle;
        parameters.subDivision = subDivision;
    }

    public void Register(ISightModule newModule)
    {
        if(modules.Contains(newModule)) return;
        modules.Add(newModule);
    }
    [ContextMenu("Initialize All Modules")]
    public void InitAllModules()
    {
        foreach(ISightModule module in modules)
        {
            module.Initialize();
        }
    }
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