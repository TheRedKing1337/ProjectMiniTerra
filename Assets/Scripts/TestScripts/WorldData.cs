using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldData
{
    //array of 6 PlanetFaces
    public PlanetFace[] faces;

    private int worldSize;

    public WorldData(int worldSize)
    {
        //Init vars
        faces = new PlanetFace[6];
        Vector3[] upVectors = new Vector3[] { Vector3.up, Vector3.back, Vector3.right, Vector3.left, Vector3.forward, Vector3.down };
        this.worldSize = worldSize;

        //For each face
        for(int i = 0; i < 6; i++)
        {
            faces[i] = new PlanetFace(worldSize, upVectors[i]);
        }
    }
    public bool BuildMesh()
    {
        Vector3[] verts = new Vector3[worldSize*worldSize*12];
        int[] tris = new int[worldSize*worldSize*18*6];

        int vertIndex = 0;
        int trisIndex = 0;

        //For each face
        foreach(PlanetFace face in faces)
        {
            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    //Set the top vertices
                    float height = face.heightMap[x, y];
                    verts[vertIndex] = face.baseVertexPos[x, y] * height;
                    verts[vertIndex + 1] = face.baseVertexPos[x + 1, y] * height;
                    verts[vertIndex + 2] = face.baseVertexPos[x, y + 1] * height;
                    verts[vertIndex+3] = face.baseVertexPos[x + 1, y + 1] * height;

                    //Set the middle vertices
                    height = (face.heightMap[x, y] - 1);
                    verts[vertIndex + 4] = face.baseVertexPos[x, y] * height;
                    verts[vertIndex + 5] = face.baseVertexPos[x + 1, y] * height;
                    verts[vertIndex + 6] = face.baseVertexPos[x, y + 1] * height;
                    verts[vertIndex + 7] = face.baseVertexPos[x + 1, y + 1] * height;

                    //Set the bottom vertices
                    height = 1;
                    verts[vertIndex + 8] = face.baseVertexPos[x, y] * height;
                    verts[vertIndex + 9] = face.baseVertexPos[x + 1, y] * height;
                    verts[vertIndex + 10] = face.baseVertexPos[x, y + 1] * height;
                    verts[vertIndex + 11] = face.baseVertexPos[x + 1, y + 1] * height;

                    //Set the top face
                    tris[trisIndex] = vertIndex;
                    tris[trisIndex + 1] = vertIndex + 1;
                    tris[trisIndex + 2] = vertIndex + 2;
                    tris[trisIndex + 3] = vertIndex + 1;
                    tris[trisIndex + 4] = vertIndex + 3;
                    tris[trisIndex + 5] = vertIndex + 2;

                    trisIndex += 6;

                    //Set the side faces, once for top sides, once for sides that go to world center
                    for (int i = 0; i < 2; i++)
                    {
                        //Back side
                        tris[trisIndex] = vertIndex;
                        tris[trisIndex + 1] = vertIndex + 4;
                        tris[trisIndex + 2] = vertIndex + 5;
                        tris[trisIndex + 3] = vertIndex + 1;
                        tris[trisIndex + 4] = vertIndex;
                        tris[trisIndex + 5] = vertIndex + 5;
                        //Right side
                        tris[trisIndex + 6] = vertIndex + 1;
                        tris[trisIndex + 7] = vertIndex + 5;
                        tris[trisIndex + 8] = vertIndex + 3;
                        tris[trisIndex + 9] = vertIndex + 3;
                        tris[trisIndex + 10] = vertIndex + 5;
                        tris[trisIndex + 11] = vertIndex + 7;
                        //Left side
                        tris[trisIndex + 12] = vertIndex;
                        tris[trisIndex + 13] = vertIndex + 2;
                        tris[trisIndex + 14] = vertIndex + 4;
                        tris[trisIndex + 15] = vertIndex + 2;
                        tris[trisIndex + 16] = vertIndex + 6;
                        tris[trisIndex + 17] = vertIndex + 4;
                        //Front side
                        tris[trisIndex + 18] = vertIndex + 2;
                        tris[trisIndex + 19] = vertIndex + 3;
                        tris[trisIndex + 20] = vertIndex + 6;
                        tris[trisIndex + 21] = vertIndex + 3;
                        tris[trisIndex + 22] = vertIndex + 7;
                        tris[trisIndex + 23] = vertIndex + 6;

                        trisIndex += 24;
                        vertIndex += 4;
                    }

                    vertIndex += 4;
                }
            }
            face.mesh.Clear();
            face.mesh.vertices = verts;
            face.mesh.triangles = tris;
            face.mesh.RecalculateNormals();

            vertIndex = 0;
            trisIndex = 0;
        }
        return false;
    }
}
public struct PlanetFace 
{
    public Vector3[,] baseVertexPos;
    public float[,] heightMap;
    public Mesh mesh;
    public Vector3[,] tempPillarPos;

    public PlanetFace(int worldSize, Vector3 localUp)
    {
        baseVertexPos = new Vector3[worldSize + 1, worldSize + 1];
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);
        //for each position on the face calculate the basePosition
        for (int x = 0; x < worldSize + 1; x++)
        {
            for (int y = 0; y < worldSize + 1; y++)
            {
                Vector2 percent = new Vector2(x, y) / (worldSize);
                Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;

                float x2 = pointOnUnitCube.x * pointOnUnitCube.x;
                float y2 = pointOnUnitCube.y * pointOnUnitCube.y;
                float z2 = pointOnUnitCube.z * pointOnUnitCube.z;

                baseVertexPos[x, y].x = pointOnUnitCube.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
                baseVertexPos[x, y].y = pointOnUnitCube.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
                baseVertexPos[x, y].z = pointOnUnitCube.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);                
            }
        }

        tempPillarPos = new Vector3[worldSize, worldSize];
        heightMap = new float[worldSize, worldSize];
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                tempPillarPos[x, y] = (baseVertexPos[x, y] + baseVertexPos[x + 1, y] + baseVertexPos[x, y + 1] + baseVertexPos[x + 1, y + 1]) / 4;
                heightMap[x, y] = worldSize + Random.Range(-2f,2);
            }
        }

        mesh = new Mesh();
    }
}

