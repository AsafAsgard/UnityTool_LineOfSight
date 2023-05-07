using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{

    [SerializeField] private LineOfSightBase losPrefab;
    [SerializeField] private int losAmount;
    [SerializeField] private float spawnRate;
    private float timeSinceLastSpawm = 0;
    LineOfSightBase[] losPool;

    private void Awake()
    {
        losPool = new LineOfSightBase[losAmount];
        for (int i = 0; i < losPool.Length; i++)
        {
            LineOfSightBase instance = Instantiate(losPrefab);
            instance.gameObject.SetActive(false);
            losPool[i] = instance;
        }
    }


    private void Update()
    {
        timeSinceLastSpawm += Time.deltaTime;
        if(timeSinceLastSpawm > spawnRate)
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        LineOfSightBase next = losPool.FirstOrDefault(o => !o.gameObject.activeSelf);
        if (next == null) return;
        next.transform.SetPositionAndRotation(transform.position, transform.rotation);
        next.gameObject.SetActive(true);


        timeSinceLastSpawm = 0;
    }
}
