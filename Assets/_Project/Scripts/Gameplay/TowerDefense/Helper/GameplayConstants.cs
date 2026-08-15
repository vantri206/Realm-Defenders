using UnityEngine;

public static class GameplayConstants
{
    public const float CELL_SIZE = 1f;
    public const float HALF_CELL_SIZE = CELL_SIZE * 0.5f;

    public const byte BLOCKED_COST = 255;
    public const float CELL_TARGET_THRESHOLD = 0.08f;
}