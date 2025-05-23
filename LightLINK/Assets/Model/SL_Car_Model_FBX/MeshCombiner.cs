using UnityEngine;
using System.Collections.Generic;

public class MeshCombiner : MonoBehaviour
{
    public enum MargeType
    {
        Hierarchy,
        MaxVertex,
        Material,
    }

    [SerializeField] private bool combineOnStart = true;
    [SerializeField] private MargeType margeType = MargeType.Material;
    private const int MaxVertexCount = 65535;
    private const int MaxMaterialCount = 128;

    [ContextMenu("Combine Meshes")]
    private void CombineMeshesFromEditor()
    {
        StartCombine();
    }

    void StartCombine()
    {
        if (combineOnStart)
        {
            if (margeType == MargeType.MaxVertex)
                CombineMeshes_MaxVertex();
            if (margeType == MargeType.Hierarchy)
                CombineMeshes_Hierarchy();
            if (margeType == MargeType.Material)
                CombineMeshes_Material();
        }
    }

    #region Material

    void CombineMeshes_Material()
    {
        // 현재 오브젝트의 자식들 중에서 MeshFilter 컴포넌트를 가진 모든 오브젝트를 리스트로 가져옵니다.
        List<MeshFilter> meshFilters = new List<MeshFilter>(GetComponentsInChildren<MeshFilter>());
        if (meshFilters.Count == 0)
        {
            // MeshFilter가 없으면 경고 메시지를 출력하고 메서드를 종료합니다.
            Debug.LogWarning("병합할 메쉬가 없습니다.");
            return;
        }

        // Material별로 CombineInstance 리스트를 저장할 딕셔너리입니다.
        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();
        // Material별로 병합된 메쉬의 총 버텍스 수를 저장할 딕셔너리입니다.
        Dictionary<Material, int> materialToVertexCount = new Dictionary<Material, int>();

        // 각 MeshFilter를 순회하며 병합 작업을 준비합니다.
        foreach (MeshFilter meshFilter in meshFilters)
        {
            // MeshFilter에 연결된 MeshRenderer를 가져옵니다.
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                // MeshRenderer나 Mesh가 없으면 경고 메시지를 출력하고 다음으로 넘어갑니다.
                Debug.LogWarning($"MeshFilter {meshFilter.name}에 MeshRenderer 또는 Mesh가 없습니다.");
                continue;
            }

            // MeshRenderer에 연결된 머티리얼 배열을 가져옵니다.
            Material[] materials = meshRenderer.sharedMaterials;

            // 각 서브메쉬와 머티리얼을 순회합니다.
            for (int i = 0; i < materials.Length && i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                // 현재 서브메쉬에 해당하는 머티리얼을 가져옵니다.
                Material material = materials[i];
                if (material == null)
                {
                    // 머티리얼이 없으면 경고 메시지를 출력하고 다음으로 넘어갑니다.
                    Debug.LogWarning($"MeshFilter {meshFilter.name}의 서브메쉬 {i}에 머티리얼이 없습니다.");
                    continue;
                }

                // CombineInstance를 생성하여 병합할 메쉬와 변환 정보를 설정합니다.
                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = meshFilter.sharedMesh, // 병합할 메쉬
                    transform = meshFilter.transform.localToWorldMatrix, // 월드 좌표계로 변환 행렬
                    subMeshIndex = i // 서브메쉬 인덱스
                };

                // 해당 머티리얼에 대한 CombineInstance 리스트가 없으면 새로 생성합니다.
                if (!materialToCombineInstances.ContainsKey(material))
                {
                    materialToCombineInstances[material] = new List<CombineInstance>();
                    materialToVertexCount[material] = 0; // 초기 버텍스 수는 0으로 설정
                }

                // 머티리얼에 해당하는 CombineInstance 리스트에 추가합니다.
                materialToCombineInstances[material].Add(combineInstance);
                // 해당 머티리얼의 총 버텍스 수를 업데이트합니다.
                materialToVertexCount[material] += meshFilter.sharedMesh.vertexCount;
            }

            // 원본 MeshFilter 오브젝트를 비활성화합니다.
            meshFilter.gameObject.SetActive(false);
        }

        int meshIndex = 0; // 병합된 메쉬의 인덱스를 추적합니다.
        foreach (var pair in materialToCombineInstances)
        {
            Material material = pair.Key; // 현재 머티리얼
            List<CombineInstance> combineInstances = pair.Value; // 해당 머티리얼에 대한 CombineInstance 리스트
            int currentVertexCount = 0; // 현재 병합 중인 메쉬의 총 버텍스 수
            List<CombineInstance> currentCombineInstances = new List<CombineInstance>(); // 현재 병합 중인 CombineInstance 리스트

            foreach (var combineInstance in combineInstances)
            {
                int vertexCount = combineInstance.mesh.vertexCount; // 현재 메쉬의 버텍스 수
                if (currentVertexCount + vertexCount > MaxVertexCount)
                {
                    // 최대 버텍스 수를 초과하면 현재까지의 CombineInstance 리스트로 병합된 메쉬를 생성합니다.
                    CreateCombinedMesh(currentCombineInstances, material, meshIndex++);
                    currentCombineInstances.Clear(); // 리스트를 초기화
                    currentVertexCount = 0; // 버텍스 수 초기화
                }

                // 현재 CombineInstance를 리스트에 추가합니다.
                currentCombineInstances.Add(combineInstance);
                // 현재 병합 중인 버텍스 수를 업데이트합니다.
                currentVertexCount += vertexCount;
            }

            if (currentCombineInstances.Count > 0)
            {
                // 남아 있는 CombineInstance 리스트로 병합된 메쉬를 생성합니다.
                CreateCombinedMesh(currentCombineInstances, material, meshIndex++);
            }
        }
    }

    private void CreateCombinedMesh(List<CombineInstance> combineInstances, Material material, int index)
    {
        GameObject combinedObject = new GameObject($"CombinedMesh_{index}");
        combinedObject.transform.SetParent(transform, false);
        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedMeshRenderer = combinedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 인덱스 형식 사용
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

        combinedMeshFilter.sharedMesh = combinedMesh;
        combinedMeshRenderer.sharedMaterial = material;
    }

    #endregion

    #region MaxVertex

    public void CombineMeshes_MaxVertex()
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>(GetComponentsInChildren<MeshFilter>());
        if (meshFilters.Count == 0)
        {
            Debug.LogWarning("병합할 메쉬가 없습니다.");
            return;
        }

        List<CombineInstance> combineInstances = new List<CombineInstance>();
        int currentVertexCount = 0;
        int meshIndex = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}에 Mesh가 없습니다.");
                continue;
            }

            int meshVertexCount = meshFilter.sharedMesh.vertexCount;
            if (currentVertexCount + meshVertexCount > MaxVertexCount)
            {
                CreateCombinedMesh(combineInstances, meshIndex++);
                combineInstances.Clear();
                currentVertexCount = 0;
            }

            for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = meshFilter.sharedMesh,
                    transform = meshFilter.transform.localToWorldMatrix,
                    subMeshIndex = i
                };

                combineInstances.Add(combineInstance);
            }

            currentVertexCount += meshVertexCount;

            // 원본 오브젝트 비활성화
            meshFilter.gameObject.SetActive(false);
        }

        if (combineInstances.Count > 0)
        {
            CreateCombinedMesh(combineInstances, meshIndex);
        }
    }

    private void CreateCombinedMesh(List<CombineInstance> combineInstances, int index)
    {
        GameObject combinedObject = new GameObject($"CombinedMesh_{index}");
        combinedObject.transform.SetParent(transform, false);
        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedMeshRenderer = combinedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 인덱스 형식 사용
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

        combinedMeshFilter.sharedMesh = combinedMesh;
        combinedMeshRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
    }

    #endregion

    #region Hierarchy

    public void CombineMeshes_Hierarchy()
    {
        List<MeshFilter> leafMeshFilters = new List<MeshFilter>();
        FindLeafMeshFilters(transform, leafMeshFilters);

        if (leafMeshFilters.Count == 0)
        {
            Debug.LogWarning("병합할 메쉬가 없습니다.");
            return;
        }

        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();
        Dictionary<Material, int> materialToVertexCount = new Dictionary<Material, int>();

        foreach (MeshFilter meshFilter in leafMeshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}에 MeshRenderer 또는 Mesh가 없습니다.");
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;

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
                    materialToVertexCount[material] = 0;
                }

                materialToCombineInstances[material].Add(combineInstance);
                materialToVertexCount[material] += meshFilter.sharedMesh.vertexCount;
            }

            // 원본 오브젝트 비활성화
            meshFilter.gameObject.SetActive(false);
        }

        if (materialToCombineInstances.Count == 0)
        {
            Debug.LogWarning("병합할 메쉬가 없습니다. 머티리얼 그룹화에 실패했습니다.");
            return;
        }

        GameObject combinedObject = new GameObject("CombinedMesh");
        combinedObject.transform.SetParent(transform, false);
        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer combinedMeshRenderer = combinedObject.AddComponent<MeshRenderer>();

        List<Material> combinedMaterials = new List<Material>();
        List<Mesh> subMeshes = new List<Mesh>();

        foreach (var pair in materialToCombineInstances)
        {
            Material material = pair.Key;
            List<CombineInstance> combineInstances = pair.Value;

            if (materialToVertexCount[material] > MaxVertexCount)
            {
                Debug.LogWarning($"머티리얼 {material.name}에 대한 병합된 메쉬의 예상 버텍스 수가 최대 지원 버텍스 수를 초과했습니다: {materialToVertexCount[material]}");
                continue;
            }

            Mesh combinedSubMesh = new Mesh();
            combinedSubMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 인덱스 형식 사용
            combinedSubMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            subMeshes.Add(combinedSubMesh);
            combinedMaterials.Add(material);
            Debug.Log($"머티리얼 {material.name}에 대해 {combineInstances.Count}개의 메쉬를 병합했습니다.");
        }

        if (subMeshes.Count == 0)
        {
            Debug.LogWarning("병합된 메쉬가 없습니다. 모든 메쉬가 최대 버텍스 수를 초과했습니다.");
            return;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 인덱스 형식 사용
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
        combinedMeshRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
    }

    private void FindLeafMeshFilters(Transform parent, List<MeshFilter> leafMeshFilters)
    {
        bool hasChildNode = false;

        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("node#"))
            {
                hasChildNode = true;
                break;
            }
        }

        if (parent.name.StartsWith("node#") && !hasChildNode)
        {
            foreach (Transform child in parent)
            {
                if (child.GetComponent<MeshFilter>() != null)
                {
                    leafMeshFilters.Add(child.GetComponent<MeshFilter>());
                }
            }
            return;
        }

        foreach (Transform child in parent)
        {
            FindLeafMeshFilters(child, leafMeshFilters);
        }
    }

    #endregion
}
