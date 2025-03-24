using UnityEngine;

public class WalkThruHandler : MonoBehaviour
{
    private const float RotateDelta = 4f;
    private float MoveDelta = 1f;
    private float MaxMoveDelta = 10f;

    void Update()
    {
        bool isMoving = false;

        if (Input.GetKey(KeyCode.W))
        {
            Vector3 p = gameObject.transform.position;
            Vector3 ff = gameObject.transform.forward;
            Vector3 dst = p + ff * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            Vector3 p = gameObject.transform.position;
            Vector3 bb = -gameObject.transform.forward;
            Vector3 dst = p + bb * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            Vector3 p = gameObject.transform.position;
            Vector3 bb = -gameObject.transform.right;
            Vector3 dst = p + bb * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 p = gameObject.transform.position;
            Vector3 bb = gameObject.transform.right;
            Vector3 dst = p + bb * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        var wheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (wheelInput != 0)
        {
            Vector3 p = gameObject.transform.position;
            Vector3 bb = gameObject.transform.forward;
            Vector3 dst = p + bb * wheelInput * 40f;
            transform.position = dst;
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Vector3 p = transform.position;
            Vector3 bb = -transform.up;
            Vector3 dst = p + bb * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 p = transform.position;
            Vector3 bb = transform.up;
            Vector3 dst = p + bb * MoveDelta;
            transform.position = dst;
            isMoving = true;
        }

        if (isMoving)
        {
            MoveDelta += 5f * Time.deltaTime;
            MoveDelta = Mathf.Min(MoveDelta, MaxMoveDelta);
        }
        else
        {
            MoveDelta = 1f;
        }

        // Right-click + mouse to rotate
        if (Input.GetMouseButton(1))
        {
            transform.eulerAngles += RotateDelta * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        }
    }
}
