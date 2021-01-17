using UnityEngine;

namespace VISVolcano
{
    public class CameraController : MonoBehaviour
    {
        [Header("Scroll variables:")]
        public float scrollSpeed = 1000;
        public float minDist = 50;
        public float maxDist = 1000;

        private Vector3 oldPos;
        private Transform cameraTransform;

        private void Awake()
        {
            cameraTransform = transform.GetChild(0).transform;
        }
        void Update()
        {
            //if scrolling
            if (Input.mouseScrollDelta.y != 0)
            {
                Vector3 newPos = cameraTransform.localPosition;
                newPos.z = Mathf.Clamp(newPos.z - (Input.mouseScrollDelta.y * scrollSpeed), minDist, maxDist);
                cameraTransform.localPosition = newPos;
            }
            else if (Input.GetMouseButtonDown(0)) { oldPos = oldPos = Input.mousePosition; }
            else if (Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - oldPos;

                transform.eulerAngles = new Vector3(transform.eulerAngles.x + delta.y, transform.eulerAngles.y + delta.x, 0);

                oldPos = Input.mousePosition;
            }

        }
    }
}
