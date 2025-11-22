using System;
using UnityEngine;

[Serializable]
public enum PlayerCommandType
{
    PointerDown,
    PointerDrag,
    PointerUp,
    Ability,
    EndTurn
}

[Serializable]
public enum PlayerCommandTarget
{
    Board,
    Puck
}

[Serializable]
public struct PlayerCommand
{
    public int PlayerId;
    public int PointerId;
    public PlayerCommandType CommandType;
    public PlayerCommandTarget Target;
    public int TargetInstanceId;
    public Vector3 WorldPosition;

    public static PlayerCommand CreatePointerCommand(
        int playerId,
        int pointerId,
        PlayerCommandType type,
        PlayerCommandTarget target,
        int targetInstanceId,
        Vector3 worldPosition)
    {
        return new PlayerCommand
        {
            PlayerId = playerId,
            PointerId = pointerId,
            CommandType = type,
            Target = target,
            TargetInstanceId = targetInstanceId,
            WorldPosition = worldPosition
        };
    }
}
