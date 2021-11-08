using UnityEngine;
using UnityEngine.UI;

public class Roll : MonoBehaviour
{
    public Image Image;
    public ThirdPersonMovement Player;

    void Update()
    {
        if (Player is null) return;

        Image.color = Color.Lerp(Color.red, Color.green, Mathf.Min(Player.dashTimer, 1));
    }
}
