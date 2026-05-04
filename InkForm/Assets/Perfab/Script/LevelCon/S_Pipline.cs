using UnityEngine;

public class S_Pipline : MonoBehaviour
{
    [SerializeField] private GameObject Output;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (Output == null) return;
        if (collision.CompareTag("Player") && S_Player.Instance.getForm())
        {
            S_Player.Instance.Teleport(Output.transform.position);
        }
    }
}