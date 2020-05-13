using JT;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
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
    }
}
