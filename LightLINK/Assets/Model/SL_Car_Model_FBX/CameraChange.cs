using UnityEngine;

public class CameraChange : MonoBehaviour
{
    public GameObject[] cameras; // Inspector���� ���� ī�޶� �Ҵ�
    private Camera activeCamera;
    int camIndex = 0;

    void Start()
    {
        // �ʱ� ī�޶� ����
        SetActiveCamera(1); //0���� ��
    }

    public void SetActiveCamera(int index)
    {
        camIndex = index > 0 ? 0 : 1;

        // ��� ī�޶� ��Ȱ��ȭ
        foreach (GameObject cam in cameras)
        {
            cam.SetActive(false);
        }

        // ������ ī�޶� Ȱ��ȭ
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
            SetActiveCamera(camIndex); // ��ġ �� ī�޶� ����
        }
    }
}
