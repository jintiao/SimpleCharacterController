using UnityEngine;
using UnityEditor;

namespace JT
{
    public class SkeletonImporter : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject root)
        {
            var skeletonRoot = root.transform.Find("Skeleton");
            
            if (skeletonRoot == null)
                return;
            if (skeletonRoot.GetComponent<MeshFilter>() || skeletonRoot.GetComponent<SkinnedMeshRenderer>() || skeletonRoot.GetComponent<Collider>())
                return;

            var skeletonComponent = root.AddComponent<Skeleton>();
            skeletonComponent.StoreBoneData(skeletonRoot);
        }
    }
}
