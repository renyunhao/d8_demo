using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class SimpleRotate : MonoBehaviour
    {
        public Space rotateSpace = Space.Self;
        public float rotateSpeed = 360;
        public Vector3 rotateAxis = new Vector3(0, 1, 0);

        void Update()
        {
            this.transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime, rotateSpace);
        }
    }
}