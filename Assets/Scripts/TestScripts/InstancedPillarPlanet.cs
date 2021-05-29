using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class InstancedPillarPlanet : MonoBehaviour
{
    [Header("Planet Settings")]
    [SerializeField] private int faceWidth = 64;
    [SerializeField] private float planetScale = 5;
    [Header("Instancing vars")]
    [SerializeField] private Material material;
    [SerializeField] private Mesh mesh;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private Bounds bounds;

    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }

    private void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Init();
        sw.Stop();
        UnityEngine.Debug.Log(sw.ElapsedMilliseconds);
    }

    private void Init()
    {
        int population = faceWidth * faceWidth * 6;
        bounds = new Bounds(transform.position, Vector3.one * faceWidth * 2);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        //Job to calculate the positions of the pillars
        //CalculatePositionsJob calculatePositionsJob = new CalculatePositionsJob
        //{
        //    population = population,
        //    faceWidth = this.faceWidth,
        //    planetScale = this.planetScale,

        //    positions = new NativeArray<Vector3>(population, Allocator.TempJob)
        //};
        //JobHandle jobHandle = calculatePositionsJob.Schedule();
        //jobHandle.Complete();

        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[population];
        for (int i = 0; i < population; i++)
        {
            MeshProperties props = new MeshProperties();
            //Vector3 position = calculatePositionsJob.positions[i];
            Vector3 position = GetPillarPosition(i);
            Quaternion rotation = GetPillarRotation(position);
            Vector3 scale = Vector3.one / 100;

            props.mat = Matrix4x4.TRS(position, rotation, scale);
            props.color = Color.Lerp(Color.red, Color.blue, Random.value);

            properties[i] = props;
        }
        //calculatePositionsJob.positions.Dispose();

        meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }
    private Quaternion GetPillarRotation(Vector3 position)
    {
        return Quaternion.Euler(transform.position - position);
    }
    private Vector3 GetPillarPosition(int i)
    {
        Vector3 localUp = GetLocalUp(i);

        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        int localIndex = i % (faceWidth * faceWidth);
        int x = localIndex % faceWidth;
        int y = Mathf.FloorToInt(localIndex / faceWidth);
        Vector2 percent = new Vector2(x, y) / (faceWidth);
        Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;

        float x2 = pointOnUnitCube.x * pointOnUnitCube.x;
        float y2 = pointOnUnitCube.y * pointOnUnitCube.y;
        float z2 = pointOnUnitCube.z * pointOnUnitCube.z;

        Vector3 pillarPosition;
        pillarPosition.x = pointOnUnitCube.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
        pillarPosition.y = pointOnUnitCube.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
        pillarPosition.z = pointOnUnitCube.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);

        return pillarPosition * planetScale;
    }
    private Vector3 GetLocalUp(int i)
    {
        int faceIndex = Mathf.FloorToInt(i / (faceWidth * faceWidth));
        switch (faceIndex)
        {
            case 0: return Vector3.up;
            case 1: return Vector3.back;
            case 2: return Vector3.right;
            case 3: return Vector3.left;
            case 4: return Vector3.forward;
            case 5: return Vector3.down;
            default: return Vector3.down;
        }
    }
    [BurstCompile]
    struct CalculatePositionsJob : IJob
    {
        public int population;
        public int faceWidth;
        public float planetScale;

        public NativeArray<Vector3> positions;
        public void Execute()
        {           
            for (int i = 0; i < population; i++)
            {
                Vector3 localUp = Vector3.zero;
                int faceIndex = Mathf.FloorToInt(i / (faceWidth * faceWidth));
                switch (faceIndex)
                {
                    case 0: localUp = Vector3.up; break;
                    case 1: localUp = Vector3.back; break;
                    case 2: localUp = Vector3.right; break;
                    case 3: localUp = Vector3.left; break;
                    case 4: localUp = Vector3.forward; break;
                    case 5: localUp = Vector3.down; break;
                }

                Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
                Vector3 axisB = Vector3.Cross(localUp, axisA);

                int localIndex = i % (faceWidth * faceWidth);
                int x = localIndex % faceWidth;
                int y = Mathf.FloorToInt(localIndex / faceWidth);
                Vector2 percent = new Vector2(x, y) / (faceWidth);
                Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;

                float x2 = pointOnUnitCube.x * pointOnUnitCube.x;
                float y2 = pointOnUnitCube.y * pointOnUnitCube.y;
                float z2 = pointOnUnitCube.z * pointOnUnitCube.z;

                Vector3 pillarPosition;
                pillarPosition.x = pointOnUnitCube.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
                pillarPosition.y = pointOnUnitCube.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
                pillarPosition.z = pointOnUnitCube.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);

                positions[i] = pillarPosition * planetScale;
            }
        }
    }
    private void OnDisable()
    {
        // Release gracefully.
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }
}
