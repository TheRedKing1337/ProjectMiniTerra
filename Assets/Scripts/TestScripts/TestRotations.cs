using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotations : MonoBehaviour
{
    [SerializeField] private float xAngle, yAngle, zAngle;
    [SerializeField] private Transform lookAt;

    void OnValidate()
    {
        transform.rotation = GetPillarRotation(lookAt.position);
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
}
