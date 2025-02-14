using UnityEngine;

namespace GameFramework
{
    public class UIRotate : MonoBehaviour
    {
        public float rotateSpeed = 360;
        public bool useRealTime;

        void Update()
        {
            this.transform.Rotate(new Vector3(0, 0, rotateSpeed) * (useRealTime ? Time.unscaledDeltaTime : Time.deltaTime));
        }
    }
}