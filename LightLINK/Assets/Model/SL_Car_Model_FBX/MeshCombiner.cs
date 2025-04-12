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
        List<MeshFilter> meshFilters = new List<MeshFilter>(GetComponentsInChildren<MeshFilter>());
        if (meshFilters.Count == 0)
        {
            Debug.LogWarning("������ �޽��� �����ϴ�.");
            return;
        }

        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();
        Dictionary<Material, int> materialToVertexCount = new Dictionary<Material, int>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}�� MeshRenderer �Ǵ� Mesh�� �����ϴ�.");
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;

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
                    materialToVertexCount[material] = 0;
                }

                materialToCombineInstances[material].Add(combineInstance);
                materialToVertexCount[material] += meshFilter.sharedMesh.vertexCount;
            }

            // ���� ������Ʈ ��Ȱ��ȭ
            meshFilter.gameObject.SetActive(false);
        }

        int meshIndex = 0;
        foreach (var pair in materialToCombineInstances)
        {
            Material material = pair.Key;
            List<CombineInstance> combineInstances = pair.Value;
            int currentVertexCount = 0;
            List<CombineInstance> currentCombineInstances = new List<CombineInstance>();

            foreach (var combineInstance in combineInstances)
            {
                int vertexCount = combineInstance.mesh.vertexCount;
                if (currentVertexCount + vertexCount > MaxVertexCount)
                {
                    CreateCombinedMesh(currentCombineInstances, material, meshIndex++);
                    currentCombineInstances.Clear();
                    currentVertexCount = 0;
                }

                currentCombineInstances.Add(combineInstance);
                currentVertexCount += vertexCount;
            }

            if (currentCombineInstances.Count > 0)
            {
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
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 �ε��� ���� ���
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
            Debug.LogWarning("������ �޽��� �����ϴ�.");
            return;
        }

        List<CombineInstance> combineInstances = new List<CombineInstance>();
        int currentVertexCount = 0;
        int meshIndex = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}�� Mesh�� �����ϴ�.");
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

            // ���� ������Ʈ ��Ȱ��ȭ
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
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 �ε��� ���� ���
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
            Debug.LogWarning("������ �޽��� �����ϴ�.");
            return;
        }

        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();
        Dictionary<Material, int> materialToVertexCount = new Dictionary<Material, int>();

        foreach (MeshFilter meshFilter in leafMeshFilters)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MeshFilter {meshFilter.name}�� MeshRenderer �Ǵ� Mesh�� �����ϴ�.");
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;

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
                    materialToVertexCount[material] = 0;
                }

                materialToCombineInstances[material].Add(combineInstance);
                materialToVertexCount[material] += meshFilter.sharedMesh.vertexCount;
            }

            // ���� ������Ʈ ��Ȱ��ȭ
            meshFilter.gameObject.SetActive(false);
        }

        if (materialToCombineInstances.Count == 0)
        {
            Debug.LogWarning("������ �޽��� �����ϴ�. ��Ƽ���� �׷�ȭ�� �����߽��ϴ�.");
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
                Debug.LogWarning($"��Ƽ���� {material.name}�� ���� ���յ� �޽��� ���� ���ؽ� ���� �ִ� ���� ���ؽ� ���� �ʰ��߽��ϴ�: {materialToVertexCount[material]}");
                continue;
            }

            Mesh combinedSubMesh = new Mesh();
            combinedSubMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 �ε��� ���� ���
            combinedSubMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            subMeshes.Add(combinedSubMesh);
            combinedMaterials.Add(material);
            Debug.Log($"��Ƽ���� {material.name}�� ���� {combineInstances.Count}���� �޽��� �����߽��ϴ�.");
        }

        if (subMeshes.Count == 0)
        {
            Debug.LogWarning("���յ� �޽��� �����ϴ�. ��� �޽��� �ִ� ���ؽ� ���� �ʰ��߽��ϴ�.");
            return;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // UInt32 �ε��� ���� ���
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
