using UnityEngine;

public class Dummy : MonoBehaviour
{
    private TextMesh Text;
    private Enemy Enemy;

    private void Awake()
    {
        Text = gameObject.GetComponentInChildren<TextMesh>();
        Enemy = gameObject.GetComponent<Enemy>();
    }

    void Update()
    {
        Text.text = $"{Enemy.PowerLevel}";    
    }
}
