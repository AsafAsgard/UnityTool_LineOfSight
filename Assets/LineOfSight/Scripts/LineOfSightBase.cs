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
    [SerializeField][Range(0, 1000)] private float maxViewDistance = 10;
    [SerializeField][Range(0, 360)] public float horizontalAngle = 30;
    [SerializeField][Range(0, 180)] public float verticalAngle = 30;
    [SerializeField, Tooltip("Controls the level of detail of the mesh")]
    [Range(1, 10)]
    private int subDivision = 3;

    private List<ISightModule> modules = new();

    [Header("Shared Paramenters")]
    [Tooltip("Set a parameters object here to have it shared with other LOS objects.\nLeave empty to generate a new parameters object for this instance.")]
    private LineOfSightParameters parameters;

    public LineOfSightParameters Parameters
    {
        get => parameters;
        set
        {
            parameters = value;
            UpdateViewDistance();
            InitAllModules();
        }
    }

    private void Awake()
    {
        CreateLayerDictionary();

        Parameters = ScriptableObject.CreateInstance<LineOfSightParameters>();
        Parameters.root = gameObject;
        Parameters.tagsDictionary = tagsDictionary;
    }
    private void CreateLayerDictionary()
    {
        tagsDictionary.Clear();
        foreach (LayerDetails layer in detectionTags)
        {
            if (layer == null) continue;
            tagsDictionary.Add(layer.targetTag, layer.distance);
        }
    }
    private void OnValidate()
    {
        if (tagsDictionary.Count == 0)
            CreateLayerDictionary();

        if (Parameters == null) return;
        if (root != null)
            Parameters.root = root;

        Parameters.maxViewDistance = maxViewDistance;
        Parameters.tagsDictionary = tagsDictionary;
        Parameters.enviromentLayers = enviromentLayers;
        Parameters.horizontalAngle = horizontalAngle;
        Parameters.verticalAngle = verticalAngle;
        Parameters.subDivision = subDivision;
    }
    private void Start()
    {
        InitAllModules();
    }


    public void SetTagDetectionDistance(string targetTag, float distance)
    {
        string layer = tagsDictionary.Keys.SingleOrDefault(dl => dl.Equals(targetTag));
        if (string.IsNullOrEmpty(layer))
            tagsDictionary.Add(targetTag, distance);
        else
            tagsDictionary[layer] = distance;
        UpdateViewDistance();
    }

    private void UpdateViewDistance()
    {
        if (tagsDictionary.Count == 0) return;

        maxViewDistance = tagsDictionary.Values.Select(dl => dl).ToList().Max();
    }




    public void Register(ISightModule newModule)
    {
        if (modules.Contains(newModule)) return;
        modules.Add(newModule);
    }
    [ContextMenu("Initialize All Modules")]
    public void InitAllModules()
    {
        if (modules.Count == 0)
        {
            foreach (ISightModule module in GetComponents<ISightModule>())
            {
                if (modules.Contains(module)) return;
                modules.Add(module);
            }
        }

        Debug.Log($"Initializing {modules.Count} modules");
        foreach (ISightModule module in modules)
        {
            module.Initialize(Parameters);
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