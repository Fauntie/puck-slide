using UnityEngine;

#if MIRROR
using Mirror;
#endif

namespace Puckslide.Networking
{
    [AddComponentMenu("Networking/Network Disconnect Handler")]
    public class NetworkDisconnectHandler : MonoBehaviour
    {
        [SerializeField]
        private NetworkSessionManager m_SessionManager;
#if MIRROR
        [SerializeField]
        private NetworkManager m_NetworkManager;
#endif

        private void Awake()
        {
            if (m_SessionManager == null)
            {
                m_SessionManager = NetworkSessionManager.Instance ?? FindObjectOfType<NetworkSessionManager>();
            }
#if MIRROR
            if (m_NetworkManager == null)
            {
                m_NetworkManager = FindObjectOfType<NetworkManager>();
            }
#endif
        }

        private void OnEnable()
        {
            NetworkEvents.OnDisconnected.AddListener(OnDisconnected);
        }

        private void OnDisable()
        {
            NetworkEvents.OnDisconnected.RemoveListener(OnDisconnected);
        }

        private void OnDisconnected(NetworkDisconnectReason reason)
        {
#if MIRROR
            if (m_NetworkManager != null)
            {
                if (m_NetworkManager.mode == NetworkManagerMode.Host)
                {
                    m_NetworkManager.StopHost();
                }
                else if (m_NetworkManager.mode == NetworkManagerMode.ServerOnly)
                {
                    m_NetworkManager.StopServer();
                }
                else if (m_NetworkManager.mode == NetworkManagerMode.ClientOnly)
                {
                    m_NetworkManager.StopClient();
                }
            }
#endif

            if (m_SessionManager == null)
            {
                m_SessionManager = NetworkSessionManager.Instance ?? FindObjectOfType<NetworkSessionManager>();
            }

            if (m_SessionManager != null)
            {
                m_SessionManager.HandleDisconnect(reason);
            }
        }
    }
}
