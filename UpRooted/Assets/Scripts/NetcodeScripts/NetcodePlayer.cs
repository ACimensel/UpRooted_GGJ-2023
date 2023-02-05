using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetcodePlayer : NetworkBehaviour
{
    // Network managed Positions.
    // Host's can modify this directly.
    // Client's must ask permission and receive the updated value before applying it.
    [NonSerialized] public readonly NetworkVariable<Vector3> Position = new();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetInitialPosition();
        }
    }

    public void SetInitialPosition()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Placeholder, random spawn location
            var randomPosition = GetRandomPositionOnPlane();
            transform.position = randomPosition;
            Position.Value = randomPosition;
        }
        else
        {
            // Ask the server for initial position
            SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc()
    {
        // Placeholder, pick random location to teleport to
        Position.Value = GetRandomPositionOnPlane();
    }

    // Placeholder, find a random position for testing purposes
    private Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
    }

    // Called every frame by Unity
    private void Update()
    {
        // Set the local game object's position to the network confirmed Position
        transform.position = Position.Value;
    }

    public void ParentToPlayer(GameObject heldItem)
    {
        heldItem.transform.SetParent(gameObject.transform);
    }
}