using UnityEngine;
using EzySlice;

public class MeshSlicer : MonoBehaviour
{
    [Header("Slice Settings")]
    [SerializeField] private Material cutMaterial;

    public void CutObject(GameObject target, Vector3 pointOnPlane, Vector3 planeNormal)
    {
        SkinnedMeshRenderer smr = target.GetComponentInChildren<SkinnedMeshRenderer>();
        GameObject sliceTarget = target;
        GameObject tempBakedObj = null;

        if (smr != null) {
            Mesh bakedMesh = new();
            smr.BakeMesh(bakedMesh);

            tempBakedObj = new GameObject("TempBakedSliceObj");
            tempBakedObj.transform.position = smr.transform.position;
            tempBakedObj.transform.rotation = smr.transform.rotation;
            tempBakedObj.transform.localScale = smr.transform.lossyScale;

            MeshFilter mf = tempBakedObj.AddComponent<MeshFilter>();
            mf.sharedMesh = bakedMesh;

            MeshRenderer mr = tempBakedObj.AddComponent<MeshRenderer>();
            mr.sharedMaterials = smr.sharedMaterials;

            sliceTarget = tempBakedObj;
        }

        SlicedHull hull = sliceTarget.Slice(pointOnPlane, planeNormal, cutMaterial);

        if (hull != null) {
            Material matToUse = cutMaterial != null ? cutMaterial : GetTargetMaterial(sliceTarget);

            GameObject upperHull = hull.CreateUpperHull(sliceTarget, matToUse);
            GameObject lowerHull = hull.CreateLowerHull(sliceTarget, matToUse);

            SetupPiece(upperHull, target);
            SetupPiece(lowerHull, target);

            Destroy(target);
            if (tempBakedObj != null) Destroy(tempBakedObj);
        } else if (tempBakedObj != null) {
            Destroy(tempBakedObj);
        }
    }

    private Material GetTargetMaterial(GameObject target)
    {
        Renderer r = target.GetComponent<Renderer>();
        return (r != null && r.sharedMaterial != null) ? r.sharedMaterial : null;
    }

    private void SetupPiece(GameObject piece, GameObject originalTarget)
    {
        piece.tag = originalTarget.tag;
        piece.layer = originalTarget.layer;

        Vector3 computedPosition = piece.transform.position;
        
        piece.transform.localScale = originalTarget.transform.lossyScale;
        piece.transform.position = originalTarget.transform.position + Vector3.Scale(computedPosition - originalTarget.transform.position, originalTarget.transform.lossyScale);

        piece.transform.SetParent(null, true);

        MeshFilter mf = piece.GetComponent<MeshFilter>();

        if (mf != null && mf.sharedMesh != null) {
            if (mf.sharedMesh.vertexCount > 256) {
                piece.AddComponent<BoxCollider>();
            } else {
                MeshCollider mc = piece.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
            }
        } else {
            piece.AddComponent<BoxCollider>();
        }

        Rigidbody rb = piece.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Vector3 explosionOrigin = piece.GetComponent<Renderer>().bounds.center - Vector3.up * 0.1f;
        rb.AddExplosionForce(120f, explosionOrigin, 1.5f, 0.5f, ForceMode.Impulse);

        Destroy(piece, 10f);
    }
}
