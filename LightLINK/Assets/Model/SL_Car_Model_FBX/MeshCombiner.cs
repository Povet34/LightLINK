using UnityEngine;
using System.Collections.Generic;

public class MeshCombiner : MonoBehaviour
{
    [SerializeField] private bool combineOnStart = true;
    [SerializeField] private Transform targetParent;

    void Start()
    {
        if (combineOnStart && targetParent != null)
        {
            CombineMeshes();
        }
    }

    public void CombineMeshes()
    {
        if (targetParent == null)
        {
            Debug.LogWarning("Target Parent�� �������� �ʾҽ��ϴ�. ������ ������Ʈ�� �������ּ���.");
            return;
        }

        MeshFilter[] meshFilters = targetParent.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("������ �޽��� �����ϴ�.");
            return;
        }

       // Debug.Log($"�� {meshFilters.Length}���� MeshFilter�� ã�ҽ��ϴ�.");

        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}�� MeshRenderer �Ǵ� Mesh�� �����ϴ�.");
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;
            //Debug.Log($"MeshFilter {meshFilter.name}�� {materials.Length}���� ��Ƽ������ �ֽ��ϴ�.");

            for (int i = 0; i < materials.Length && i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    Debug.LogWarning($"MeshFilter {meshFilter.name}�� ����޽� {i}�� ��Ƽ������ �����ϴ�.");
                    continue;
                }

                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = meshFilter.sharedMesh,
                    transform = meshFilter.transform.localToWorldMatrix,
                    subMeshIndex = i
                };

                if (!materialToCombineInstances.ContainsKey(material))
                {
                    materialToCombineInstances[material] = new List<CombineInstance>();
                }
                materialToCombineInstances[material].Add(combineInstance);
            }

            // ���� ������Ʈ ��Ȱ��ȭ (������� ���� �ּ� ó�� ����)
            // meshFilter.gameObject.SetActive(false);
        }

        if (materialToCombineInstances.Count == 0)
        {
            Debug.LogWarning("������ �޽��� �����ϴ�. ��Ƽ���� �׷�ȭ�� �����߽��ϴ�.");
            return;
        }

        GameObject combinedObject = new GameObject("CombinedMesh");
        combinedObject.transform.SetParent(targetParent, false);
        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedMeshRenderer = combinedObject.AddComponent<MeshRenderer>();

        List<Material> combinedMaterials = new List<Material>();
        List<Mesh> subMeshes = new List<Mesh>();

        foreach (var pair in materialToCombineInstances)
        {
            Material material = pair.Key;
            List<CombineInstance> combineInstances = pair.Value;

            Mesh combinedSubMesh = new Mesh();
            combinedSubMesh.CombineMeshes(combineInstances.ToArray(), true, true);
            subMeshes.Add(combinedSubMesh);
            combinedMaterials.Add(material);
            Debug.Log($"��Ƽ���� {material.name}�� ���� {combineInstances.Count}���� �޽��� �����߽��ϴ�.");
        }

        Mesh finalMesh = new Mesh();
        CombineInstance[] finalCombineInstances = new CombineInstance[subMeshes.Count];
        for (int i = 0; i < subMeshes.Count; i++)
        {
            finalCombineInstances[i] = new CombineInstance
            {
                mesh = subMeshes[i],
                transform = Matrix4x4.identity
            };
        }

        finalMesh.CombineMeshes(finalCombineInstances, false, false);
        combinedMeshFilter.sharedMesh = finalMesh;
        combinedMeshRenderer.sharedMaterials = combinedMaterials.ToArray();

        Debug.Log($"�޽� ���� �Ϸ�: {combinedObject.name}, ����޽� ��: {finalMesh.subMeshCount}, ��Ƽ���� ��: {combinedMaterials.Count}");
        Debug.Log($"���յ� ������Ʈ ��ġ: {combinedObject.transform.position}");
    }

    [ContextMenu("Combine Meshes")]
    private void CombineMeshesFromEditor()
    {
        CombineMeshes();
    }
}