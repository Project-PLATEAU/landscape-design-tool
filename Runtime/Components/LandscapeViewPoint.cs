using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LandscapeViewPoint : MonoBehaviour
{
    [SerializeField]
    private float fov = 60.0f;

    public string Name => gameObject.name;

    public float Fov
    {
        get => fov;
        set => fov = value;
    }

    public Camera Camera
    {
        get
        {
            var camera = GetComponent<Camera>();
            if (camera == null)
                camera = gameObject.AddComponent<Camera>();
            camera.enabled = false;
            return camera;
        }
    }

    private void Reset()
    {
        Camera.enabled = false;
    }
}
