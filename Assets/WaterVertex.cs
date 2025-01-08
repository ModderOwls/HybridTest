using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterVertex : MonoBehaviour
{
    public float width = 0.1f;
    public float speed = 0.5f;

    [HideInInspector] public float distance;

    void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position + transform.right * width, transform.position - transform.right * width);
    }
}
