using Cinemachine;
using JT;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
    public CinemachineVirtualCamera vcamera;
    public GameObject character;
    public GameObject weapon;

    public float mouseSensitivity = 1.5f;

    void Start()
    {
        var c = Instantiate(character);
        var w = Instantiate(weapon);
        var a = w.AddComponent<CharacterAttachment>();
        a.attachPoint = c.GetComponent<SimpleCharacterController>().weaponAttachBone;

        vcamera.Follow = c.transform;
        vcamera.LookAt = c.transform;
    }

    void Update()
    {
        UpdateInput(ref UserCommand.defaultCommand);
    }

    private void UpdateInput(ref UserCommand command)
    {
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float angle = Vector2.Angle(Vector2.up, moveInput);
        if (moveInput.x < 0)
            angle = 360 - angle;
        float magnitude = Mathf.Clamp(moveInput.magnitude, 0, 1);
        command.moveYaw = angle;
        command.moveMagnitude = magnitude;

        var deltaMousePos = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        command.lookYaw += (deltaMousePos.x * mouseSensitivity) % 360;
        command.lookYaw = command.lookYaw % 360;
        while (command.lookYaw < 0) command.lookYaw += 360;
        command.lookPitch += deltaMousePos.y * mouseSensitivity;
        command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);

        command.jump = Input.GetKeyDown(KeyCode.Space);
        command.boost = Input.GetKeyDown(KeyCode.LeftControl);
    }
}
