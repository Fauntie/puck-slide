using System.Collections.Generic;
using UnityEngine;

public static class PuckControllerRouteHub
{
    private static readonly Dictionary<int, PuckController> s_Pucks = new Dictionary<int, PuckController>();

    public static void Register(PuckController controller)
    {
        int id = controller.GetInstanceID();
        s_Pucks[id] = controller;
    }

    public static void Unregister(PuckController controller)
    {
        int id = controller.GetInstanceID();
        if (s_Pucks.ContainsKey(id))
        {
            s_Pucks.Remove(id);
        }
    }

    public static void Process(PlayerCommand command)
    {
        if (!s_Pucks.TryGetValue(command.TargetInstanceId, out PuckController controller))
        {
            Debug.LogWarning($"No puck found for command target {command.TargetInstanceId}");
            return;
        }

        controller.ProcessCommand(command);
    }
}
