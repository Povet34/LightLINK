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
            Debug.LogWarning("Target Parent가 설정되지 않았습니다. 병합할 오브젝트를 지정해주세요.");
            return;
        }

        MeshFilter[] meshFilters = targetParent.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("병합할 메쉬가 없습니다.");
            return;
        }

       // Debug.Log($"총 {meshFilters.Length}개의 MeshFilter를 찾았습니다.");

        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}에 MeshRenderer 또는 Mesh가 없습니다.");
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;
            //Debug.Log($"MeshFilter {meshFilter.name}에 {materials.Length}개의 머티리얼이 있습니다.");

            for (int i = 0; i < materials.Length && i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    Debug.LogWarning($"MeshFilter {meshFilter.name}의 서브메쉬 {i}에 머티리얼이 없습니다.");
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

            // 원본 오브젝트 비활성화 (디버깅을 위해 주석 처리 가능)
            // meshFilter.gameObject.SetActive(false);
        }

        if (materialToCombineInstances.Count == 0)
        {
            Debug.LogWarning("병합할 메쉬가 없습니다. 머티리얼 그룹화에 실패했습니다.");
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
            Debug.Log($"머티리얼 {material.name}에 대해 {combineInstances.Count}개의 메쉬를 병합했습니다.");
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

        Debug.Log($"메쉬 병합 완료: {combinedObject.name}, 서브메쉬 수: {finalMesh.subMeshCount}, 머티리얼 수: {combinedMaterials.Count}");
        Debug.Log($"병합된 오브젝트 위치: {combinedObject.transform.position}");
    }

    [ContextMenu("Combine Meshes")]
    private void CombineMeshesFromEditor()
    {
        CombineMeshes();
    }
}