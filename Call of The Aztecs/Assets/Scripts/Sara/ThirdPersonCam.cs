using UnityEngine;
using UnityEngine.UIElements;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Refrences")]
    public Transform orientation;
    public Transform Player;
    public Transform PlayerObject;
    public Rigidbody rb;

    public float rotationSpeed;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        //rotation direction
        Vector3 viewDirection = Player.position - new Vector3(transform.position.x, Player.position.y, transform.position.z);
        orientation.forward = viewDirection.normalized;

        //rotate player object
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDirection != Vector3.zero)
        {
            PlayerObject.forward = Vector3.Slerp(PlayerObject.forward, inputDirection.normalized, Time.deltaTime * rotationSpeed);
        }
    }
}
