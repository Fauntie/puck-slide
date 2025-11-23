#if MIRROR
using Mirror;
using UnityEngine;

namespace Puckslide.Networking
{
    [AddComponentMenu("Networking/Puckslide Network Manager")]
    public class PuckslideNetworkManager : NetworkManager
    {
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();

            Debug.Log("[Networking] Client disconnected from server.");
            NetworkEvents.OnDisconnected.Invoke(NetworkDisconnectReason.RemoteClosed);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            Debug.Log($"[Networking] Client {conn.connectionId} disconnected from host.");
            NetworkEvents.OnDisconnected.Invoke(NetworkDisconnectReason.RemoteClosed);
        }

        public override void OnClientError(TransportError error, string message)
        {
            base.OnClientError(error, message);
            Debug.LogWarning($"[Networking] Client transport error: {error} {message}");
            NetworkEvents.OnDisconnected.Invoke(NetworkDisconnectReason.TransportError);
        }
    }
}
#endif
