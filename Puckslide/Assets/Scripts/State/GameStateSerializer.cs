using UnityEngine;

public static class GameStateSerializer
{
    public static string ToJson(GameStateSnapshot snapshot, bool prettyPrint = false)
    {
        return JsonUtility.ToJson(snapshot, prettyPrint);
    }

    public static GameStateSnapshot FromJson(string json)
    {
        return JsonUtility.FromJson<GameStateSnapshot>(json);
    }
}
