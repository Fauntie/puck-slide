using System.Collections.Generic;
using Puckslide.Networking;
using UnityEngine;

public class SimulationCommandProcessor : MonoBehaviour
{
    [SerializeField]
    private int m_LocalPlayerId;
    [SerializeField]
    private BoardController m_BoardController;
    [SerializeField]
    private PlayerCommandDispatcher m_Dispatcher;
    [SerializeField]
    private bool m_LogProcessing;

    private readonly List<PlayerCommand> m_CommandBuffer = new List<PlayerCommand>(32);

    private void Awake()
    {
        if (m_Dispatcher == null)
        {
            m_Dispatcher = PlayerCommandDispatcher.Instance ?? FindObjectOfType<PlayerCommandDispatcher>();
        }

        if (m_BoardController == null)
        {
            m_BoardController = FindObjectOfType<BoardController>();
        }
    }

    private void FixedUpdate()
    {
        if (m_Dispatcher == null)
        {
            return;
        }

        NetworkSessionManager manager = NetworkSessionManager.Instance;
        if (manager != null && !manager.OfflineMode && !manager.IsHost)
        {
            // Non-host clients rely on snapshots from the host instead of running the simulation loop locally.
            return;
        }

        m_Dispatcher.DrainQueue(m_LocalPlayerId, m_CommandBuffer);

        foreach (PlayerCommand command in m_CommandBuffer)
        {
            if (m_LogProcessing)
            {
                Debug.Log($"Processing {command.CommandType} for target {command.Target} at {command.WorldPosition}");
            }

            switch (command.Target)
            {
                case PlayerCommandTarget.Board:
                    m_BoardController?.ProcessCommand(command);
                    break;
                case PlayerCommandTarget.Puck:
                    PuckControllerRouteHub.Process(command);
                    break;
            }
        }
    }
}
