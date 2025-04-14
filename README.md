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
  
