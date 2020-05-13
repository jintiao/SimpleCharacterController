using System;
using System.Collections.Generic;
using UnityEngine;

namespace JT
{
    public class Skeleton : MonoBehaviour
    {
        public Transform[] bones;
        public int[] nameHashes;
        public int[] parentIndex;
        public Bonepose[] importPose;

        public bool StoreBoneData(Transform skeletonRoot)
        {
            if (!skeletonRoot)
            {
                bones = new Transform[0];
                nameHashes = new int[0];
                parentIndex = new int[0];
                importPose = new Bonepose[0];
                return false;
            }

            var boneList = new List<Transform>();
            GetBones(skeletonRoot, skeletonRoot, ref boneList);

            var numBones = boneList.Count;
            bones = boneList.ToArray();

            nameHashes = new int[numBones];
            importPose = new Bonepose[numBones];

            for (var i = 0; i < numBones; i++)
            {
                string boneName = bones[i].gameObject.name;
                int hashCode = boneName.GetHashCode();
                nameHashes[i] = hashCode;
                var bindpose = new Bonepose
                {
                    localPosition = bones[i].localPosition,
                    localRotation = bones[i].localRotation,
                    localScale = bones[i].localScale
                };

                importPose[i] = bindpose;
            }

            parentIndex = new int[numBones];
            for (var i = 0; i < numBones; i++)
            {
                parentIndex[i] = GetBoneIndex(bones[i].parent.gameObject.name.GetHashCode());
            }
            return true;
        }

        public int GetBoneIndex(int stringHash)
        {
            var numBones = bones.Length;
            for (var i = 0; i < numBones; i++)
            {
                if (nameHashes[i] == stringHash)
                    return i;
            }

            return -1;
        }

        static void GetBones(Transform t, Transform skeletonRoot, ref List<Transform> boneList)
        {
            var bonesToProcess = new Queue<Transform>();
            bonesToProcess.Enqueue(t);

            while (bonesToProcess.Count > 0)
            {
                var currentBone = bonesToProcess.Dequeue();
                boneList.Add(currentBone);

                var numChildren = currentBone.childCount;
                for (var i = 0; i < numChildren; i++)
                {
                    bonesToProcess.Enqueue(currentBone.GetChild(i));
                }
            }
        }

        [Serializable]
        public struct Bonepose
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }
    }
}
