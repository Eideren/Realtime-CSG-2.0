using System;
using UnityEngine;

namespace InternalRealtimeCSG
{
    [Serializable]
    internal class TransformSelection
    {
        [SerializeField] public Transform[] transforms = Array.Empty<Transform>();        // all transforms
        [SerializeField] public Vector3[] backupPositions = Array.Empty<Vector3>();       // all transforms
        [SerializeField] public Quaternion[] backupRotations = Array.Empty<Quaternion>(); // all transforms

        public void Select(Transform[] transforms)
        {
            this.transforms = transforms;
            backupPositions = new Vector3[this.transforms.Length];
            backupRotations = new Quaternion[this.transforms.Length];

            for(int i = 0; i < this.transforms.Length; i++)
            {
                backupPositions[i] = this.transforms[i].position;
                backupRotations[i] = this.transforms[i].rotation;
            }
        }

        public void Update()
        {
            for(int t = 0; t < transforms.Length; t++)
            {
                if(!transforms[t])
                {
                    continue;
                }

                backupPositions[t] = transforms[t].position;
                backupRotations[t] = transforms[t].rotation;
            }
        }
    }
}
