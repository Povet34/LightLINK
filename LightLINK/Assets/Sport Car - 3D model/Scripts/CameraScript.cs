using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {
    
    private bool MouseClick = false;
    public bool AutoRotate = false;
    public float cameraRotateSpeed = 5f;
    public Transform RotateTarget;
    public float rotationSpeed = 6f; // 초당 회전 속도 (분당 360도 = 초당 6도)

    void Start () 
    {
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (AutoRotate && RotateTarget != null)
        {
            // RotateAround을 사용해 RotateTarget 주위를 자동 회전
            // Time.deltaTime을 곱해 프레임 독립적인 회전을 구현
            transform.RotateAround(RotateTarget.position, RotateTarget.up, rotationSpeed * Time.deltaTime);
        }
        else
        {
            //Rotation
            if (Input.GetMouseButtonDown(0)) MouseClick = true;
            if (Input.GetMouseButtonUp(0)) MouseClick = false;

            if (MouseClick) transform.RotateAround(RotateTarget.position, RotateTarget.up, Input.GetAxis("Mouse X") * cameraRotateSpeed);
        }
    }
}
