using UnityEngine;

namespace DeepShift.Camera
{
    /// <summary>
    /// Smoothly follows a target Transform, keeping the camera's Z position fixed.
    /// Attach to the Main Camera. Assign the player GameObject as <see cref="_target"/>.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float     _smoothSpeed = 8f;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desired = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
        }
    }
}
