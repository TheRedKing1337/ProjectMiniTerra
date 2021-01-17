using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TRKGeneric;
using UnityEditor;
using System;

public class WorldManager : MonoSingleton<WorldManager>
{
    public int worldSize = 10;
    public Material tempMat;

    WorldData worldData;

    private void Start()
    {
        worldData = new WorldData(worldSize);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            worldData.BuildMesh();
            for (int i = 0; i < 6; i++)
            {
                GameObject go = new GameObject(i.ToString());
                go.transform.parent = transform;
                go.AddComponent<MeshFilter>().sharedMesh = worldData.faces[i].mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = tempMat;
            }
            Debug.Log("Added meshes");
        }
    }
    private void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying)
        {
            //Draw all the base vertex positions
            foreach (PlanetFace face in worldData.faces)
            {
                Gizmos.color = Color.green;
                for (int x = 0; x < worldSize + 1; x++)
                {
                    for (int y = 0; y < worldSize + 1; y++)
                    {
                        Gizmos.DrawSphere(face.baseVertexPos[x, y], 0.05f);
                    }
                }
                Gizmos.color = Color.red;
                for (int x = 0; x < worldSize; x++)
                {
                    for (int y = 0; y < worldSize; y++)
                    {
                        Gizmos.DrawSphere(face.tempPillarPos[x, y], 0.1f);
                    }
                }
            }
        }
    }
}
