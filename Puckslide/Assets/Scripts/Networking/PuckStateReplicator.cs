using System.Collections.Generic;
using UnityEngine;

namespace Puckslide.Networking
{
    public class PuckStateReplicator : MonoBehaviour
    {
        [SerializeField]
        private float m_PositionLerpSpeed = 10f;
        [SerializeField]
        private float m_VelocityLerpSpeed = 6f;

        private readonly Dictionary<int, PuckState> m_TargetStates = new Dictionary<int, PuckState>();

        private void OnEnable()
        {
            NetworkEvents.OnPuckSnapshot.AddListener(OnSnapshot);
            NetworkEvents.OnNetworkPuckSpawned.AddListener(OnSpawned);
        }

        private void OnDisable()
        {
            NetworkEvents.OnPuckSnapshot.RemoveListener(OnSnapshot);
            NetworkEvents.OnNetworkPuckSpawned.RemoveListener(OnSpawned);
            m_TargetStates.Clear();
        }

        private void Update()
        {
            NetworkSessionManager manager = NetworkSessionManager.Instance;
            if (manager != null && manager.IsHost)
            {
                return;
            }

            foreach (KeyValuePair<int, PuckState> kvp in m_TargetStates)
            {
                if (!PuckControllerRouteHub.TryGet(kvp.Key, out PuckController controller))
                {
                    continue;
                }

                Rigidbody2D body = controller.Rigidbody;
                if (body == null)
                {
                    continue;
                }

                body.position = Vector2.Lerp(body.position, kvp.Value.Position, Time.deltaTime * m_PositionLerpSpeed);
                body.velocity = Vector2.Lerp(body.velocity, kvp.Value.Velocity, Time.deltaTime * m_VelocityLerpSpeed);
                body.angularVelocity = kvp.Value.AngularVelocity;
                controller.transform.rotation = Quaternion.Euler(0f, 0f, kvp.Value.RotationZ);
            }
        }

        private void OnSnapshot(PuckStateSnapshotMessage message)
        {
            NetworkSessionManager manager = NetworkSessionManager.Instance;
            if (manager != null && manager.IsHost)
            {
                return;
            }

            if (message == null || message.Pucks == null)
            {
                return;
            }

            m_TargetStates.Clear();
            foreach (PuckState state in message.Pucks)
            {
                m_TargetStates[state.InstanceId] = state;
            }
        }

        private void OnSpawned(PuckSpawnMessage message)
        {
            NetworkSessionManager manager = NetworkSessionManager.Instance;
            if (manager != null && manager.IsHost)
            {
                return;
            }

            if (message == null)
            {
                return;
            }

            if (PuckControllerRouteHub.TryGet(message.NetworkInstanceId, out PuckController controller))
            {
                Rigidbody2D body = controller.Rigidbody;
                if (body != null)
                {
                    body.position = message.Position;
                    body.velocity = message.Velocity;
                    controller.transform.position = message.Position;
                }
            }
        }
    }
}
