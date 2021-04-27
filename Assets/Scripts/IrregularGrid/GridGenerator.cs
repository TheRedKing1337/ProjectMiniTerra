using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Unity.Collections;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid vars")]
    [SerializeField] private int hexChunkSize = 5;
    [SerializeField] [Range(0, 20)] private float hexChunkScale = 1;
    [Header("Debug vars")]
    [SerializeField] private bool useLinear;
    [SerializeField] private float debugSphereRadius = 1;

    private List<Vector2> gridPoints = new List<Vector2>();
    private void OnValidate()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if(useLinear) gridPoints = GenerateHexGridLinear(hexChunkSize, hexChunkScale);
        else gridPoints = GenerateHexGrid(hexChunkSize, hexChunkScale);
        stopwatch.Stop();
        UnityEngine.Debug.Log((useLinear ? "Linear" : "Radius method") + " took " + stopwatch.ElapsedMilliseconds + " ms to complete");
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, debugSphereRadius);
        for (int i = 0; i < gridPoints.Count; i++)
        {
            Gizmos.DrawSphere(transform.position+(Vector3)gridPoints[i], debugSphereRadius);
        }
    }

    private List<Vector2> GenerateHexGrid(int numLayers, float scale)
    {
        List<Vector2> gridPoints = new List<Vector2>();
        for (int layer = 1; layer < numLayers + 1; layer++) //foreach layer, start at 1 because every instance needs it at + 1
        {
            //Gets the distance from the centerPos for the point calculation
            float radius = scale * layer;
            for (int p = 0; p < 6; p++) //foreach of the 6 points
            {
                //Calc corner pos and add to List
                Vector2 pPos = GetHexPos(radius, p);
                gridPoints.Add(pPos);

                //Calc all positions along line
                Vector2 nextPPos = GetHexPos(radius, p + 1);
                float lineLength = 1f / layer;
                for (int linePosI = 1; linePosI < layer; linePosI++)
                {
                    Vector2 linePos = Vector2.Lerp(pPos, nextPPos, lineLength * linePosI);
                    gridPoints.Add(linePos);
                }
            }
        }
        return gridPoints;
    }
    private List<Vector2> GenerateHexGridLinear(int numLayers, float scale)
    {
        List<Vector2> gridPoints = new List<Vector2>();
        //For each vertical layer, start at bottom
        for(int y = -numLayers; y < numLayers+ 1; y++)
        {
            //For each horizontal layer, start at left
            float rightMostX = (numLayers + numLayers - Mathf.Abs(y) )/ 2f;
            for(float x = -rightMostX; x <= rightMostX; x++)
            {
                gridPoints.Add(new Vector2(x, y* 0.85714285714285714285714285714286f) *scale);
            }
        }
        return gridPoints;
    }
    private Vector2 GetHexPos(float radius, int pointIndex)
    {
        //60 degree angle in radians
        float angle = 1.047197544f * pointIndex;
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;
        return new Vector2(x, y);
    }
}
