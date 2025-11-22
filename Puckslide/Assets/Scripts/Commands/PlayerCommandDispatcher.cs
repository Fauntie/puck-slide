using System.Collections.Generic;
using UnityEngine;

public class PlayerCommandDispatcher : MonoBehaviour
{
    [SerializeField]
    private bool m_LogCommands;

    private readonly Dictionary<int, Queue<PlayerCommand>> m_CommandQueues = new Dictionary<int, Queue<PlayerCommand>>();

    public static PlayerCommandDispatcher Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Enqueue(PlayerCommand command)
    {
        if (!m_CommandQueues.TryGetValue(command.PlayerId, out Queue<PlayerCommand> queue))
        {
            queue = new Queue<PlayerCommand>();
            m_CommandQueues[command.PlayerId] = queue;
        }

        queue.Enqueue(command);

        if (m_LogCommands)
        {
            Debug.Log($"Enqueued {command.CommandType} for player {command.PlayerId} targeting {command.Target}");
        }
    }

    public void DrainQueue(int playerId, List<PlayerCommand> buffer)
    {
        buffer.Clear();

        if (!m_CommandQueues.TryGetValue(playerId, out Queue<PlayerCommand> queue))
        {
            return;
        }

        while (queue.Count > 0)
        {
            buffer.Add(queue.Dequeue());
        }
    }

    public int PendingCommands(int playerId)
    {
        return m_CommandQueues.TryGetValue(playerId, out Queue<PlayerCommand> queue) ? queue.Count : 0;
    }
}
