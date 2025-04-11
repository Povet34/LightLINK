using UnityEngine;

public class CameraChange : MonoBehaviour
{
    public GameObject[] cameras; // Inspector에서 여러 카메라 할당
    private Camera activeCamera;
    int camIndex = 0;

    void Start()
    {
        // 초기 카메라 설정
        SetActiveCamera(1); //0으로 들어감
    }

    public void SetActiveCamera(int index)
    {
        camIndex = index > 0 ? 0 : 1;

        // 모든 카메라 비활성화
        foreach (GameObject cam in cameras)
        {
            cam.SetActive(false);
        }

        // 선택한 카메라 활성화
        if (camIndex >= 0 && camIndex < cameras.Length)
        {
            //activeCamera = cameras[camIndex];
            cameras[camIndex].SetActive(true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetActiveCamera(camIndex); // 터치 시 카메라 변경
        }
    }
}
