using UnityEngine;

public class S_Pipline : MonoBehaviour
{
    [SerializeField] private GameObject Output;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (Output == null) return;
        if (S_PlayerLookup.TryGet(collision, out IPlayerActor player) && player.IsFluidForm)
        {
            player.Teleport(Output.transform.position);
        }
    }
}
