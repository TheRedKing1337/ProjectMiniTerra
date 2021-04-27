using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class GridGeneratorJobs : MonoBehaviour
{
    [Header("Grid vars")]
    [SerializeField] private int hexChunkSize = 5;
    [SerializeField] [Range(0, 20)] private float hexChunkScale = 1;
    [Header("Debug vars")]
    [SerializeField] private bool useJob;
    [SerializeField] private float debugSphereRadius = 1;

    private NativeArray<Vector2> gridPoints = new NativeArray<Vector2>();
    private void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if (useJob)
        {
            GenerateHexGridJob hexGridJob = new GenerateHexGridJob()
            {
                numLayers = hexChunkSize,
                scale = hexChunkScale,
                gridPoints = new NativeArray<Vector2>(GetNumOfPoints(hexChunkSize), Allocator.TempJob)
            };
            JobHandle jobHandle = hexGridJob.Schedule();
            jobHandle.Complete();

            gridPoints = hexGridJob.gridPoints;
        }
        else gridPoints = GenerateHexGridLinear(hexChunkSize, hexChunkScale);
        stopwatch.Stop();
        UnityEngine.Debug.Log((useJob ? "Jobs" : "NativeArray method") + " took " + stopwatch.ElapsedMilliseconds + " ms to complete");
    }
    private void OnDestroy()
    {
        gridPoints.Dispose();
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, debugSphereRadius);
        for (int i = 0; i < gridPoints.Length; i++)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)gridPoints[i], debugSphereRadius);
        }
    }

    //Hella fast
    [BurstCompile]
    struct GenerateHexGridJob : IJob
    {
        [ReadOnly] public int numLayers;
        [ReadOnly] public float scale;

        public NativeArray<Vector2> gridPoints;
        public void Execute()
        {
            int index = 0;
            //For each vertical layer, start at bottom
            for (int y = -numLayers; y < numLayers + 1; y++)
            {
                //For each horizontal layer, start at left
                float rightMostX = (numLayers + numLayers - Mathf.Abs(y)) / 2f;
                for (float x = -rightMostX; x <= rightMostX; x++)
                {
                    gridPoints[index] = new Vector2(x, y * 0.85714285714285714285714285714286f) * scale;
                    index++;
                }
            }
        }
    }
    private NativeArray<Vector2> GenerateHexGridLinear(int numLayers, float scale)
    {
        NativeArray<Vector2> gridPoints = new NativeArray<Vector2>(GetNumOfPoints(numLayers), Allocator.TempJob);
        int index = 0;
        //For each vertical layer, start at bottom
        for (int y = -numLayers; y < numLayers + 1; y++)
        {
            //For each horizontal layer, start at left
            float rightMostX = (numLayers + numLayers - Mathf.Abs(y)) / 2f;
            for (float x = -rightMostX; x <= rightMostX; x++)
            {
                gridPoints[index] = new Vector2(x, y * 0.85714285714285714285714285714286f) * scale;
                index++;
            }
        }
        return gridPoints;
    }
    private static Vector2 GetHexPos(float radius, int pointIndex)
    {
        //60 degree angle in radians
        float angle = 1.047197544f * pointIndex;
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;
        return new Vector2(x, y);
    }
    private static int GetNumOfPoints(int numLayers)
    {
        return 1 + numLayers * 6 * numLayers;
    }
}
