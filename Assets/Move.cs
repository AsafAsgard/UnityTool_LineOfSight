using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField][Range(-5, 5)] private float speed = 0;


    private void Update()
    {
        transform.position += Vector3.forward * speed * Time.deltaTime;
    }
}
