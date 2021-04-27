using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonalSphereGenerator : MonoBehaviour
{
    [SerializeField] private int depth = 1;
    [SerializeField] private float debugSphereRadius = 0.5f;
    private List<Vector3D> points = new List<Vector3D>();
    private void OnValidate()
    {
        points.Clear();
        InitSphere(ref points, depth);
        Debug.Log(points.Count);
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.color = new Color(i/960f,1,1);
            Gizmos.DrawSphere(points[i], debugSphereRadius);
            Gizmos.color = new Color(1, i / 960f, i / 960f);
            if (i != points.Count-1)
                Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }
    public struct Vector3D
    {
        double x, y, z;

        public Vector3D normalized
        {
            get
            {
                double m = magnitude;
                return new Vector3D(x / magnitude, y / magnitude, z / magnitude);
            }
        }
        public double magnitude => System.Math.Sqrt(x * x + y * y + z * z);
        public Vector3D zero => new Vector3D(0, 0, 0);
        public Vector3D(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static implicit operator Vector3(Vector3D content)
        {
            return new Vector3
            {
                x = (float)content.x,
                y = (float)content.y,
                z = (float)content.z
            };
        }
    }
    private void Subdivide(Vector3D v1, Vector3D v2, Vector3D v3, ref List<Vector3D> sphere_points, int depth)
    {
        if (depth == 0)
        {
            sphere_points.Add(v1);
            sphere_points.Add(v2);
            sphere_points.Add(v3);
            return;
        }
        Vector3D v12 = (v1 + v2).normalized;
        Vector3D v23 = (v2 + v3).normalized;
        Vector3D v31 = (v3 + v1).normalized;
        Subdivide(v1, v12, v31, ref sphere_points, depth - 1);
        Subdivide(v2, v23, v12, ref sphere_points, depth - 1);
        Subdivide(v3, v31, v23, ref sphere_points, depth - 1);
        Subdivide(v12, v23, v31, ref sphere_points, depth - 1);
    }

    void InitSphere(ref List<Vector3D> sphere_points, int depth)
    {
        const double X = 0.525731112119133606;
        const double Z = 0.850650808352039932;
        Vector3D[] vdata = new Vector3D[] {
        new Vector3D(-X, 0.0, Z ), new Vector3D( X, 0.0, Z ), new Vector3D( -X, 0.0, -Z ), new Vector3D(X, 0.0, -Z ),
        new Vector3D( 0.0, Z, X ), new Vector3D(0.0, Z, -X ), new Vector3D(0.0, -Z, X ), new Vector3D(0.0, -Z, -X ),
        new Vector3D(Z, X, 0.0 ), new Vector3D(-Z, X, 0.0 ), new Vector3D(Z, -X, 0.0 ), new Vector3D(-Z, -X, 0.0 )
    };
        int[,] tindices = {
        { 0, 4, 1}, { 0, 9, 4 }, { 9, 5, 4 }, { 4, 5, 8 }, { 4, 8, 1 },
        { 8, 10, 1 }, { 8, 3, 10 }, { 5, 3, 8 }, { 5, 2, 3 }, { 2, 7, 3 },
        { 7, 10, 3 }, { 7, 6, 10 }, { 7, 11, 6 }, { 11, 0, 6 }, { 0, 1, 6 },
        { 6, 1, 10 }, { 9, 0, 11 }, { 9, 11, 2 }, { 9, 2, 5 }, { 7, 2, 11 }
    };
        for (int i = 0; i < 20; i++)
            Subdivide(vdata[tindices[i, 0]], vdata[tindices[i, 1]], vdata[tindices[i, 2]], ref sphere_points, depth);
    }
}
