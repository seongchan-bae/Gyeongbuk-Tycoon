using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Vector3 touchStartPos;

    void Update()
    {
        MoveScene();
    }

    private void MoveScene()
    {
        float dragSpeed = 1.0f; // 카메라 이동 속도

        // 마우스 우클릭(1) 눌렀을 때의 월드 좌표 저장
        if (Input.GetMouseButtonDown(1))
        {
            touchStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        // 우클릭을 누르고 있는 동안 카메라 이동
        if (Input.GetMouseButton(1))
        {
            Vector3 currentTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = touchStartPos - currentTouchPos;
            direction.z = 0f; // 2D Z축 고정

            Camera.main.transform.position += direction * dragSpeed;
        }
    }
}
