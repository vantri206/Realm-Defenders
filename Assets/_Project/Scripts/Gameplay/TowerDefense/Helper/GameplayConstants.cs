using UnityEngine;

public static class GameplayConstants
{
    public const float CELL_SIZE = 1f;
    public const float HALF_CELL_SIZE = CELL_SIZE * 0.5f;

    public const byte BLOCKED_COST = 255;
    public const float CELL_TARGET_THRESHOLD = 0.08f;

    public const float SECOND = 1;
    public const float MINUTE = 60 * SECOND;

    public const int NORMAL_ENEMY_LIVES_DAMAGE = 1;

    public const int MAX_FOOD = 999;

    public const float ACTION_SPEED_MULTIPLIER = 0.25f;
}