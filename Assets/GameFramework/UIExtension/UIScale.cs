using UnityEngine;

namespace GameFramework
{
    public class UIScale : MonoBehaviour
    {
        public float scaleStart = 1;
        public float scaleEnd = 0.7f;
        public float speed = 1;

        private float scaleDir;
        private float scaleMin;
        private float scaleMax;

        private float dir;

        private void Start()
        {
            scaleMin = Mathf.Min(scaleStart, scaleEnd);
            scaleMax = Mathf.Max(scaleStart, scaleEnd);
            this.transform.localScale = Vector3.one * scaleStart;
            scaleDir = scaleMax - scaleMin;
            dir = -scaleDir;
        }

        void Update()
        {
            float scale = this.transform.localScale.x;
            dir = (scale <= scaleMin) ? scaleDir : ((scale >= scaleMax) ? -scaleDir : dir);
            this.transform.localScale += Vector3.one * dir * Time.deltaTime * speed;
        }
    }
}