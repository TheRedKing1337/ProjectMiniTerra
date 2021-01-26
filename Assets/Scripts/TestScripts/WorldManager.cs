using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TRKGeneric;
using UnityEditor;
using System;

public class WorldManager : MonoSingleton<WorldManager>
{
    public int worldSize = 8;
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
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            worldData.BuildPlanet(true);
            watch.Stop();
            Debug.Log("Time multithreaded: "+ watch.ElapsedMilliseconds / 1000f);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            worldData.BuildPlanet(false);
            watch.Stop();
            Debug.Log("Time singlethreaded: " + watch.ElapsedMilliseconds / 1000f);
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
            }
        }
    }
}
