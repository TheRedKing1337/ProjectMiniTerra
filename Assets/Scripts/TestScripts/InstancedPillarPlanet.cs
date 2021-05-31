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
    [SerializeField] private bool computePosAndRot = true;
    [SerializeField] private Material material;
    [SerializeField] private Mesh mesh;
    [SerializeField] private ComputeShader compute;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private int population;
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
        population = faceWidth * faceWidth * 6;
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
        if (computePosAndRot)
        {
            for (int i = 0; i < population; i++)
            {
                MeshProperties props = new MeshProperties();
                //Vector3 position = calculatePositionsJob.positions[i];
                Vector3 position = Vector3.zero;//Vector3.zero; // GetPillarPosition(i);
                Quaternion rotation = Quaternion.identity;// Quaternion.identity; // GetPillarRotation(position);
                Vector3 scale = Vector3.one / 100;

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = new Color(Random.value, Random.value, Random.value);

                properties[i] = props;
            }
            //calculatePositionsJob.positions.Dispose();

            meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
            meshPropertiesBuffer.SetData(properties);

            int kernel = compute.FindKernel("CSMain");
            compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

            compute.SetVector("_CenterPosition", transform.position);
            compute.SetFloat("_PlanetScale", planetScale);
            compute.SetInt("_FaceWidth", faceWidth);
            compute.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);
        } else
        {
            for (int i = 0; i < population; i++)
            {
                MeshProperties props = new MeshProperties();
                //Vector3 position = calculatePositionsJob.positions[i];
                Vector3 position = GetPillarPosition(i);//Vector3.zero; // GetPillarPosition(i);
                Quaternion rotation = GetPillarRotation(position);// Quaternion.identity; // GetPillarRotation(position);
                Vector3 scale = Vector3.one / 100;

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = Color.Lerp(Color.red, Color.blue, Random.value);

                properties[i] = props;
            }
            //calculatePositionsJob.positions.Dispose();

            meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
            meshPropertiesBuffer.SetData(properties);
        }
        //meshPropertiesBuffer.GetData(properties);
        //for (int i = 0; i < population; i++)
        //{
        //    UnityEngine.Debug.Log(new Vector3(properties[i].mat[0, 3], properties[i].mat[1, 3], properties[i].mat[2, 3]));
        //}
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    private void Update()
    {
        //int kernel = compute.FindKernel("CSMain");
        //compute.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }
    private Quaternion GetPillarRotation(Vector3 position)
    {// your code from before
        Vector3 F = (transform.position - position).normalized;   // lookAt
        Vector3 R = Vector3.Cross(Vector3.up, F).normalized; // sideaxis
        Vector3 U = Vector3.Cross(F, R);                  // rotatedup

        // note that R needed to be re-normalized
        // since F and worldUp are not necessary perpendicular
        // so must remove the sin(angle) factor of the cross-product
        // same not true for U because dot(R, F) = 0

        // adapted source
        Quaternion q;
        float trace = R.x + U.y + F.z;
        if (trace > 0.0f)
        {
            float s = 0.5f / Mathf.Sqrt(trace + 1.0f);
            q.w = 0.25f / s;
            q.x = (U.z - F.y) * s;
            q.y = (F.x - R.z) * s;
            q.z = (R.y - U.x) * s;
        }
        else
        {
            if (R.x > U.y && R.x > F.z)
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + R.x - U.y - F.z);
                q.w = (U.z - F.y) / s;
                q.x = 0.25f * s;
                q.y = (U.x + R.y) / s;
                q.z = (F.x + R.z) / s;
            }
            else if (U.y > F.z)
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + U.y - R.x - F.z);
                q.w = (F.x - R.z) / s;
                q.x = (U.x + R.y) / s;
                q.y = 0.25f * s;
                q.z = (F.y + U.z) / s;
            }
            else
            {
                float s = 2.0f * Mathf.Sqrt(1.0f + F.z - R.x - U.y);
                q.w = (R.y - U.x) / s;
                q.x = (F.x + R.z) / s;
                q.y = (F.y + U.z) / s;
                q.z = 0.25f * s;
            }
        }
        //Vector3 delta = (transform.position - position).normalized;
        ////Vector2 deltaHorizontal = new Vector2(delta.x, delta.z).normalized;
        ////Vector2 deltaVertical = new Vector2(delta.x, delta.y).normalized;
        ////float zAngle = Mathf.Atan(deltaHorizontal.x/deltaHorizontal.y) * Mathf.Rad2Deg;
        ////float yAngle = Mathf.Atan(deltaVertical.x/deltaVertical.y) * Mathf.Rad2Deg;
        //float y = Mathf.Atan2(delta.x, -delta.y) * Mathf.Rad2Deg;
        //float p = Mathf.Atan2(Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y), delta.z) * Mathf.Rad2Deg;
        return q;
        //return Quaternion.LookRotation(delta, Vector3.up);
    }
    private Vector3 GetPillarPosition(int i)
    {
        Vector3 localUp = GetLocalUp(i);

        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        int localIndex = i % (faceWidth * faceWidth);
        int x = localIndex % faceWidth;
        int y = Mathf.FloorToInt(localIndex / faceWidth);
        Vector2 percent = new Vector2(x, y) / (faceWidth - 1);
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
