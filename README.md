# LightLINK
LightLINK

자동차 및 자동차 전등 퀄리티업 및 최적화 여정

### 초기상태
- Stat


  https://github.com/user-attachments/assets/85f0e556-5271-4855-9842-4287fa0ac805

  - 자동차의 램프영역에 빛이 들어오지 않음
  - 30Fps 이하

- 프로파일링 결과
![image](https://github.com/user-attachments/assets/914c2bae-f581-49ae-bcda-e6946e662b8a)
  - RenderLoop에서 30.88ms 사용중


### Frame저하의 원인
- 차체의 Mesh가 너무 많이 분할되어있음
  ![image](https://github.com/user-attachments/assets/e73fdddc-7b98-4372-a6d1-6226e4c0c7bc)
- 셰이더의 Keywords가 달라서 Batching에 실패함
  ![image](https://github.com/user-attachments/assets/3603f3f7-55cc-4348-9dd2-d35e9f9f397a)


----
### 개선

1. Mesh Combine
   - Static Batching을 사용해도 많은 SRP Batcher가 생성됨. (올바른 선택이 아님)
   - Lamp(Emission), 차창(Transparnets), 차체(순수 Albedo)의 Material 차이가 있고, Texture는 없기 때문에 메시를 병합하는 방법을 선택 
   - 수많은 Mesh들을 같은 Material들로 묶고, 묶은 Mesh들 중에 버텍의 갯수가 65535가 넘지 않는 선에서 또 다시 묶어서 조합한다.
   - Mesh병합 및 PP적용


    https://github.com/user-attachments/assets/d6114dd5-1ad4-42dd-81f2-5c10061032c4

   ![image](https://github.com/user-attachments/assets/3fc35477-e7cd-44cf-84b4-1313081bbf58)
   ![image](https://github.com/user-attachments/assets/11a63956-1e9a-4726-85e2-aeac9b26240d)
    - 두개의 SRP Batcher로 나누어진(Emission과 Non Emission) Draw Opaque 부분

   - 하지만 여전히 RenderLoop의 시간이 길어서 CPU가 기다리고 있는 상태.


  ```C#

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

  ```

