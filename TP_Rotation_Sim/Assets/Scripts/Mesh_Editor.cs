using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mesh_Editor : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            // local → world
            /*
            Vector3 worldV = transform.TransformPoint(vertices[i]);

            // do world-space math
      
            worldV *= 4;
         
            

            // world → local
            vertices[i] = transform.InverseTransformPoint(worldV);
            */
            vertices[i] *= 4;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void Update()
    {
        
    }
}
