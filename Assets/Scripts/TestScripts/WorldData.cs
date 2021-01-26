using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WorldData
{
    //array of 6 PlanetFaces
    public PlanetFace[] faces;

    private GameObject chunkHolder;
    private int worldSize;

    //
    // Summary:
    //     Creates a new World.
    //
    // Parameters:
    //   worldSize:
    //     Size of world, must be divisible by 8.
    //
    public WorldData(int worldSize)
    {
        if (worldSize % 8 != 0)
        {
            throw new System.Exception("worldSize must be divisible by 8");
        }

        //Init vars
        faces = new PlanetFace[6];
        Vector3[] upVectors = new Vector3[] { Vector3.up, Vector3.back, Vector3.right, Vector3.left, Vector3.forward, Vector3.down };
        chunkHolder = new GameObject("ChunkHolder");
        this.worldSize = worldSize;

        //For each face
        for (int i = 0; i < 6; i++)
        {
            faces[i] = new PlanetFace(worldSize, upVectors[i]);
        }
    }
    public struct ChunkMeshData
    {
        public Vector3[] verts;
        public int[] tris;
        public Vector2[] uvs;
    }
    public bool BuildPlanet(bool threaded)
    {
        int width = worldSize / 8;
        int chunksPerFace = width * width;
        int totalNumChunks = 6 * chunksPerFace;

        ChunkMeshData[] meshData = new ChunkMeshData[totalNumChunks];
        int tempIndex = 0;

        if (!threaded)
        {
            //for each face
            for (int f = 0; f < 6; f++)
            {
                //for each chunk in face
                for (int x = 0; x < worldSize / 8; x++)
                {
                    for (int y = 0; y < worldSize / 8; y++)
                    {
                        //calc vertex, tris and uvs
                        meshData[tempIndex] = BuildChunk(f, x, y, false);
                        tempIndex++;
                    }
                }
            }
        }
        else
        {
            Parallel.For(0, totalNumChunks, index =>
            {
                int f = index / chunksPerFace;
                int x = (index / width) % width;
                int y = index % width;
                meshData[index] = BuildChunk(f, x, y, false);
            });
        }

        //for each face
        tempIndex = 0;
        for (int f = 0; f < 6; f++)
        {
            //for each chunk in face
            for (int x = 0; x < worldSize / 8; x++)
            {
                for (int y = 0; y < worldSize / 8; y++)
                {
                    PlanetChunk chunk = faces[f].chunks[x, y];
                    //mesh assignment from vars
                    //assign those to the mesh
                    if (chunk.mesh == null)
                    {
                        chunk.mesh = new Mesh();
                    }
                    else
                    {
                        chunk.mesh.Clear();
                    }
                    chunk.mesh.vertices = meshData[tempIndex].verts;
                    chunk.mesh.triangles = meshData[tempIndex].tris;
                    chunk.mesh.uv = meshData[tempIndex].uvs;
                    chunk.mesh.RecalculateNormals();

                    //create object and assign values to it
                    if (chunk.go == null)
                    {
                        chunk.go = new GameObject(f + "|" + x + "," + y);
                        chunk.go.AddComponent<MeshFilter>().sharedMesh = chunk.mesh;
                        chunk.go.AddComponent<MeshRenderer>().sharedMaterial = Resources.Load("Planet") as Material;
                        chunk.go.transform.SetParent(chunkHolder.transform);
                    }
                    else
                    {
                        chunk.go.GetComponent<MeshFilter>().sharedMesh = chunk.mesh;
                    }
                    tempIndex++;
                }
            }
        }

        return true;
    }
    public ChunkMeshData BuildChunk(int faceIndex, int chunkX, int chunkY, bool threaded)
    {
        //Init vars for for loop
        int chunkSize = 8;
        ChunkMeshData data = new ChunkMeshData();
        Vector3[] verts = new Vector3[chunkSize * chunkSize * 20];
        int[] tris = new int[chunkSize * chunkSize * 18 * 3];
        Vector2[] uvs = new Vector2[chunkSize * chunkSize * 20];

        PlanetFace face = faces[faceIndex];
        int xOffset = chunkX * 8;
        int yOffset = chunkY * 8;
        float uvStepSize = 1f / 16;

        if (threaded)
        {
            //Create verts, tris and uvs
            Parallel.For(0, chunkSize * chunkSize, index =>
            {
                //Debug.Log(Thread.CurrentThread.ManagedThreadId);

                //init local vars
                int vertIndex = index * 12;
                int trisIndex = index * 18 * 3;

                //Get the position in the data array 
                int xPos = (index % 8) + xOffset;
                int yPos = Mathf.FloorToInt(index / 8f) + yOffset;

                //Set the top vertices
                float height = face.heightMap[xPos, yPos];
                verts[vertIndex] = face.baseVertexPos[xPos, yPos] * height;
                verts[vertIndex + 1] = face.baseVertexPos[xPos + 1, yPos] * height;
                verts[vertIndex + 2] = face.baseVertexPos[xPos, yPos + 1] * height;
                verts[vertIndex + 3] = face.baseVertexPos[xPos + 1, yPos + 1] * height;

                //Set these values based on pillarType later, for now default to grass
                float topCornerX = uvStepSize * 2;
                float topCornerY = uvStepSize * 14;
                uvs[vertIndex] = new Vector2(topCornerX, topCornerY);
                uvs[vertIndex + 1] = new Vector2(topCornerX + uvStepSize, topCornerY);
                uvs[vertIndex + 2] = new Vector2(topCornerX, topCornerY - uvStepSize);
                uvs[vertIndex + 3] = new Vector2(topCornerX + uvStepSize, topCornerY - uvStepSize);

                //Set the middle vertices, double all because uvs
                height = (face.heightMap[xPos, yPos] - 1);
                verts[vertIndex + 4] = face.baseVertexPos[xPos, yPos] * height;
                verts[vertIndex + 5] = face.baseVertexPos[xPos, yPos] * height;
                verts[vertIndex + 6] = face.baseVertexPos[xPos + 1, yPos] * height;
                verts[vertIndex + 7] = face.baseVertexPos[xPos + 1, yPos] * height;
                verts[vertIndex + 8] = face.baseVertexPos[xPos + 1, yPos + 1] * height;
                verts[vertIndex + 9] = face.baseVertexPos[xPos + 1, yPos + 1] * height;
                verts[vertIndex + 10] = face.baseVertexPos[xPos, yPos + 1] * height;
                verts[vertIndex + 11] = face.baseVertexPos[xPos, yPos + 1] * height;

                float doubleStep = uvStepSize * 2;
                uvs[vertIndex + 4] = new Vector2(topCornerX - uvStepSize, topCornerY);
                uvs[vertIndex + 5] = new Vector2(topCornerX, topCornerY + uvStepSize);
                uvs[vertIndex + 6] = new Vector2(topCornerX + uvStepSize, topCornerY + uvStepSize);
                uvs[vertIndex + 7] = new Vector2(topCornerX + doubleStep, topCornerY);
                uvs[vertIndex + 8] = new Vector2(topCornerX + doubleStep, topCornerY - uvStepSize);
                uvs[vertIndex + 9] = new Vector2(topCornerX + uvStepSize, topCornerY - doubleStep);
                uvs[vertIndex + 10] = new Vector2(topCornerX, topCornerY - doubleStep);
                uvs[vertIndex + 11] = new Vector2(topCornerX - uvStepSize, topCornerY - uvStepSize);

                //Set the bottom vertices, double all because uvs
                verts[vertIndex + 12] = face.baseVertexPos[xPos, yPos];
                verts[vertIndex + 13] = face.baseVertexPos[xPos, yPos];
                verts[vertIndex + 14] = face.baseVertexPos[xPos + 1, yPos];
                verts[vertIndex + 15] = face.baseVertexPos[xPos + 1, yPos];
                verts[vertIndex + 16] = face.baseVertexPos[xPos + 1, yPos + 1];
                verts[vertIndex + 17] = face.baseVertexPos[xPos + 1, yPos + 1];
                verts[vertIndex + 18] = face.baseVertexPos[xPos, yPos + 1];
                verts[vertIndex + 19] = face.baseVertexPos[xPos, yPos + 1];

                uvStepSize *= 2;
                doubleStep *= 2;
                uvs[vertIndex + 12] = new Vector2(topCornerX - uvStepSize, topCornerY);
                uvs[vertIndex + 13] = new Vector2(topCornerX, topCornerY + uvStepSize);
                uvs[vertIndex + 14] = new Vector2(topCornerX + uvStepSize, topCornerY + uvStepSize);
                uvs[vertIndex + 15] = new Vector2(topCornerX + doubleStep, topCornerY);
                uvs[vertIndex + 16] = new Vector2(topCornerX + doubleStep, topCornerY - uvStepSize);
                uvs[vertIndex + 17] = new Vector2(topCornerX + uvStepSize, topCornerY - doubleStep);
                uvs[vertIndex + 18] = new Vector2(topCornerX, topCornerY - doubleStep);
                uvs[vertIndex + 19] = new Vector2(topCornerX - uvStepSize, topCornerY - uvStepSize);
                uvStepSize *= 0.5f;

                //Set the top face
                tris[trisIndex] = vertIndex;
                tris[trisIndex + 1] = vertIndex + 1;
                tris[trisIndex + 2] = vertIndex + 2;
                tris[trisIndex + 3] = vertIndex + 1;
                tris[trisIndex + 4] = vertIndex + 3;
                tris[trisIndex + 5] = vertIndex + 2;

                trisIndex += 6;

                //Set the side faces, once for top sides, once for sides that go to world center

                //Back side
                tris[trisIndex] = vertIndex;
                tris[trisIndex + 1] = vertIndex + 5;
                tris[trisIndex + 2] = vertIndex + 6;
                tris[trisIndex + 3] = vertIndex + 1;
                tris[trisIndex + 4] = vertIndex;
                tris[trisIndex + 5] = vertIndex + 6;
                //Right side
                tris[trisIndex + 6] = vertIndex + 1;
                tris[trisIndex + 7] = vertIndex + 7;
                tris[trisIndex + 8] = vertIndex + 8;
                tris[trisIndex + 9] = vertIndex + 3;
                tris[trisIndex + 10] = vertIndex + 1;
                tris[trisIndex + 11] = vertIndex + 8;
                //Front side
                tris[trisIndex + 12] = vertIndex + 3;
                tris[trisIndex + 13] = vertIndex + 9;
                tris[trisIndex + 14] = vertIndex + 10;
                tris[trisIndex + 15] = vertIndex + 2;
                tris[trisIndex + 16] = vertIndex + 3;
                tris[trisIndex + 17] = vertIndex + 10;
                //Left side
                tris[trisIndex + 18] = vertIndex + 2;
                tris[trisIndex + 19] = vertIndex + 11;
                tris[trisIndex + 20] = vertIndex + 4;
                tris[trisIndex + 21] = vertIndex;
                tris[trisIndex + 22] = vertIndex + 2;
                tris[trisIndex + 23] = vertIndex + 4;

                //Bottom side faces
                //Back side
                tris[trisIndex + 24] = vertIndex + 5;
                tris[trisIndex + 25] = vertIndex + 13;
                tris[trisIndex + 26] = vertIndex + 14;
                tris[trisIndex + 27] = vertIndex + 6;
                tris[trisIndex + 28] = vertIndex + 5;
                tris[trisIndex + 29] = vertIndex + 14;
                //Right side
                tris[trisIndex + 30] = vertIndex + 7;
                tris[trisIndex + 31] = vertIndex + 15;
                tris[trisIndex + 32] = vertIndex + 16;
                tris[trisIndex + 33] = vertIndex + 8;
                tris[trisIndex + 34] = vertIndex + 7;
                tris[trisIndex + 35] = vertIndex + 16;
                //Front side
                tris[trisIndex + 36] = vertIndex + 9;
                tris[trisIndex + 37] = vertIndex + 17;
                tris[trisIndex + 38] = vertIndex + 18;
                tris[trisIndex + 39] = vertIndex + 10;
                tris[trisIndex + 40] = vertIndex + 9;
                tris[trisIndex + 41] = vertIndex + 18;
                //Left side
                tris[trisIndex + 42] = vertIndex + 11;
                tris[trisIndex + 43] = vertIndex + 19;
                tris[trisIndex + 44] = vertIndex + 12;
                tris[trisIndex + 45] = vertIndex + 4;
                tris[trisIndex + 46] = vertIndex + 11;
                tris[trisIndex + 47] = vertIndex + 12;

                vertIndex += 20;
                trisIndex += 48;
            });
        }
        else
        {
            int vertIndex = 0;
            int trisIndex = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    //Get the position in the data array 
                    int xPos = x + xOffset;
                    int yPos = y + yOffset;

                    //Set the top vertices
                    float height = face.heightMap[xPos, yPos];
                    verts[vertIndex] = face.baseVertexPos[xPos, yPos] * height;
                    verts[vertIndex + 1] = face.baseVertexPos[xPos + 1, yPos] * height;
                    verts[vertIndex + 2] = face.baseVertexPos[xPos, yPos + 1] * height;
                    verts[vertIndex + 3] = face.baseVertexPos[xPos + 1, yPos + 1] * height;

                    //Set these values based on pillarType later, for now default to grass
                    float topCornerX = uvStepSize * 2;
                    float topCornerY = uvStepSize * 14;
                    uvs[vertIndex] = new Vector2(topCornerX, topCornerY);
                    uvs[vertIndex + 1] = new Vector2(topCornerX + uvStepSize, topCornerY);
                    uvs[vertIndex + 2] = new Vector2(topCornerX, topCornerY - uvStepSize);
                    uvs[vertIndex + 3] = new Vector2(topCornerX + uvStepSize, topCornerY - uvStepSize);

                    //Set the middle vertices, double all because uvs
                    height = (face.heightMap[xPos, yPos] - 1);
                    verts[vertIndex + 4] = face.baseVertexPos[xPos, yPos] * height;
                    verts[vertIndex + 5] = face.baseVertexPos[xPos, yPos] * height;
                    verts[vertIndex + 6] = face.baseVertexPos[xPos + 1, yPos] * height;
                    verts[vertIndex + 7] = face.baseVertexPos[xPos + 1, yPos] * height;
                    verts[vertIndex + 8] = face.baseVertexPos[xPos + 1, yPos + 1] * height;
                    verts[vertIndex + 9] = face.baseVertexPos[xPos + 1, yPos + 1] * height;
                    verts[vertIndex + 10] = face.baseVertexPos[xPos, yPos + 1] * height;
                    verts[vertIndex + 11] = face.baseVertexPos[xPos, yPos + 1] * height;

                    float doubleStep = uvStepSize * 2;
                    uvs[vertIndex + 4] = new Vector2(topCornerX - uvStepSize, topCornerY);
                    uvs[vertIndex + 5] = new Vector2(topCornerX, topCornerY + uvStepSize);
                    uvs[vertIndex + 6] = new Vector2(topCornerX + uvStepSize, topCornerY + uvStepSize);
                    uvs[vertIndex + 7] = new Vector2(topCornerX + doubleStep, topCornerY);
                    uvs[vertIndex + 8] = new Vector2(topCornerX + doubleStep, topCornerY - uvStepSize);
                    uvs[vertIndex + 9] = new Vector2(topCornerX + uvStepSize, topCornerY - doubleStep);
                    uvs[vertIndex + 10] = new Vector2(topCornerX, topCornerY - doubleStep);
                    uvs[vertIndex + 11] = new Vector2(topCornerX - uvStepSize, topCornerY - uvStepSize);

                    //Set the bottom vertices, double all because uvs
                    verts[vertIndex + 12] = face.baseVertexPos[xPos, yPos];
                    verts[vertIndex + 13] = face.baseVertexPos[xPos, yPos];
                    verts[vertIndex + 14] = face.baseVertexPos[xPos + 1, yPos];
                    verts[vertIndex + 15] = face.baseVertexPos[xPos + 1, yPos];
                    verts[vertIndex + 16] = face.baseVertexPos[xPos + 1, yPos + 1];
                    verts[vertIndex + 17] = face.baseVertexPos[xPos + 1, yPos + 1];
                    verts[vertIndex + 18] = face.baseVertexPos[xPos, yPos + 1];
                    verts[vertIndex + 19] = face.baseVertexPos[xPos, yPos + 1];

                    uvStepSize *= 2;
                    doubleStep *= 2;
                    uvs[vertIndex + 12] = new Vector2(topCornerX - uvStepSize, topCornerY);
                    uvs[vertIndex + 13] = new Vector2(topCornerX, topCornerY + uvStepSize);
                    uvs[vertIndex + 14] = new Vector2(topCornerX + uvStepSize, topCornerY + uvStepSize);
                    uvs[vertIndex + 15] = new Vector2(topCornerX + doubleStep, topCornerY);
                    uvs[vertIndex + 16] = new Vector2(topCornerX + doubleStep, topCornerY - uvStepSize);
                    uvs[vertIndex + 17] = new Vector2(topCornerX + uvStepSize, topCornerY - doubleStep);
                    uvs[vertIndex + 18] = new Vector2(topCornerX, topCornerY - doubleStep);
                    uvs[vertIndex + 19] = new Vector2(topCornerX - uvStepSize, topCornerY - uvStepSize);
                    uvStepSize *= 0.5f;

                    //Set the top face
                    tris[trisIndex] = vertIndex;
                    tris[trisIndex + 1] = vertIndex + 1;
                    tris[trisIndex + 2] = vertIndex + 2;
                    tris[trisIndex + 3] = vertIndex + 1;
                    tris[trisIndex + 4] = vertIndex + 3;
                    tris[trisIndex + 5] = vertIndex + 2;

                    trisIndex += 6;

                    //Set the side faces, once for top sides, once for sides that go to world center

                    //Back side
                    tris[trisIndex] = vertIndex;
                    tris[trisIndex + 1] = vertIndex + 5;
                    tris[trisIndex + 2] = vertIndex + 6;
                    tris[trisIndex + 3] = vertIndex + 1;
                    tris[trisIndex + 4] = vertIndex;
                    tris[trisIndex + 5] = vertIndex + 6;
                    //Right side
                    tris[trisIndex + 6] = vertIndex + 1;
                    tris[trisIndex + 7] = vertIndex + 7;
                    tris[trisIndex + 8] = vertIndex + 8;
                    tris[trisIndex + 9] = vertIndex + 3;
                    tris[trisIndex + 10] = vertIndex + 1;
                    tris[trisIndex + 11] = vertIndex + 8;
                    //Front side
                    tris[trisIndex + 12] = vertIndex + 3;
                    tris[trisIndex + 13] = vertIndex + 9;
                    tris[trisIndex + 14] = vertIndex + 10;
                    tris[trisIndex + 15] = vertIndex + 2;
                    tris[trisIndex + 16] = vertIndex + 3;
                    tris[trisIndex + 17] = vertIndex + 10;
                    //Left side
                    tris[trisIndex + 18] = vertIndex + 2;
                    tris[trisIndex + 19] = vertIndex + 11;
                    tris[trisIndex + 20] = vertIndex + 4;
                    tris[trisIndex + 21] = vertIndex;
                    tris[trisIndex + 22] = vertIndex + 2;
                    tris[trisIndex + 23] = vertIndex + 4;

                    //Bottom side faces
                    //Back side
                    tris[trisIndex + 24] = vertIndex + 5;
                    tris[trisIndex + 25] = vertIndex + 13;
                    tris[trisIndex + 26] = vertIndex + 14;
                    tris[trisIndex + 27] = vertIndex + 6;
                    tris[trisIndex + 28] = vertIndex + 5;
                    tris[trisIndex + 29] = vertIndex + 14;
                    //Right side
                    tris[trisIndex + 30] = vertIndex + 7;
                    tris[trisIndex + 31] = vertIndex + 15;
                    tris[trisIndex + 32] = vertIndex + 16;
                    tris[trisIndex + 33] = vertIndex + 8;
                    tris[trisIndex + 34] = vertIndex + 7;
                    tris[trisIndex + 35] = vertIndex + 16;
                    //Front side
                    tris[trisIndex + 36] = vertIndex + 9;
                    tris[trisIndex + 37] = vertIndex + 17;
                    tris[trisIndex + 38] = vertIndex + 18;
                    tris[trisIndex + 39] = vertIndex + 10;
                    tris[trisIndex + 40] = vertIndex + 9;
                    tris[trisIndex + 41] = vertIndex + 18;
                    //Left side
                    tris[trisIndex + 42] = vertIndex + 11;
                    tris[trisIndex + 43] = vertIndex + 19;
                    tris[trisIndex + 44] = vertIndex + 12;
                    tris[trisIndex + 45] = vertIndex + 4;
                    tris[trisIndex + 46] = vertIndex + 11;
                    tris[trisIndex + 47] = vertIndex + 12;

                    vertIndex += 20;
                    trisIndex += 48;
                }
            }
        }

        data.verts = verts;
        data.tris = tris;
        data.uvs = uvs;

        return data;
    }
}
public class PlanetChunk
{
    public Mesh mesh;
    public GameObject go;
    public Vector3 pos;
}
public class PlanetFace
{
    public Vector3[,] baseVertexPos;
    public float[,] heightMap;
    public PlanetChunk[,] chunks;

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

        heightMap = new float[worldSize, worldSize];
        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                heightMap[x, y] = worldSize + Random.Range(-2f, 2);
            }
        }

        int numChunks = worldSize / 8;
        chunks = new PlanetChunk[numChunks, numChunks];
        for (int x = 0; x < numChunks; x++)
        {
            for (int y = 0; y < numChunks; y++)
            {
                chunks[x, y] = new PlanetChunk();
                chunks[x, y].mesh = new Mesh();
            }
        }
    }
}

