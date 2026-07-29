using UnityEngine;

public static class GameLayer
{
    // 1. LAYER INDICES

    public static readonly int PlayerIndex = LayerMask.NameToLayer("Player");
    public static readonly int EnemyIndex = LayerMask.NameToLayer("Enemy");
    public static readonly int NeutralIndex = LayerMask.NameToLayer("Neutral");
    public static readonly int HitboxIndex = LayerMask.NameToLayer("Hitbox");
    public static readonly int HurtboxIndex = LayerMask.NameToLayer("Hurtbox");
    public static readonly int ProjectileIndex = LayerMask.NameToLayer("Projectile");
    public static readonly int ObstacleIndex = LayerMask.NameToLayer("Obstacle");
    public static readonly int NoPhysicsIndex = LayerMask.NameToLayer("NoPhysics");

    // 2. LAYER MASKS

    public static readonly LayerMask PlayerMask = 1 << PlayerIndex;
    public static readonly LayerMask EnemyMask = 1 << EnemyIndex;
    public static readonly LayerMask NeutralMask = 1 << NeutralIndex;
    public static readonly LayerMask HitboxMask = 1 << HitboxIndex;
    public static readonly LayerMask HurtboxMask = 1 << HurtboxIndex;
    public static readonly LayerMask ProjectileMask = 1 << ProjectileIndex;
    public static readonly LayerMask ObstacleMask = 1 << ObstacleIndex;
    public static readonly LayerMask NoPhysicsMask = 1 << NoPhysicsIndex;

    // 3. LAYER MASK GROUP CHECK

    public static readonly LayerMask PlayerAttackTargetMask = EnemyMask | HurtboxMask;

    public static readonly LayerMask EnemyAttackTargetMask = PlayerMask | HurtboxMask;

    static GameLayer()
    {
#if UNITY_EDITOR
        ValidateLayer(PlayerIndex, "Player");
        ValidateLayer(EnemyIndex, "Enemy");
        ValidateLayer(NeutralIndex, "Neutral");
        ValidateLayer(HitboxIndex, "Hitbox");
        ValidateLayer(HurtboxIndex, "Hurtbox");
        ValidateLayer(ProjectileIndex, "Projectile");
        ValidateLayer(ObstacleIndex, "Obstacle");
        ValidateLayer(NoPhysicsIndex, "NoPhysics");
#endif
    }

#if UNITY_EDITOR
    private static void ValidateLayer(int index, string layerName)
    {
        if (index == -1)
        {
            Debug.LogError($"[GameLayers] LAYER NOT FOUND: '{layerName}'!");
        }
    }
#endif
}

public static class LayerMaskExtensions
{
    public static bool ContainsLayer(this LayerMask mask, int layerIndex)
    {
        return (mask.value & (1 << layerIndex)) != 0;
    }
}