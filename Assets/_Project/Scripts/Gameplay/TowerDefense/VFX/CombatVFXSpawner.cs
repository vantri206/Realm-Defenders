using UnityEngine;

public static class CombatVFXSpawner
{
    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Vector3 position)
    {
        return SpawnSimpleSpriteVFX(prefab, position, Quaternion.identity);
    }

    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[CombatVFXSpawner] SimpleSpriteAnimatorVFX prefab is null. Cannot spawn VFX.", prefab);
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, position, rotation);
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Vector3 position)
    {
        return SpawnParticleVFX(prefab, position, Quaternion.identity);
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[CombatVFXSpawner] ParticleVFX prefab is null. Cannot spawn VFX.", prefab);
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, position, rotation);
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Hurtbox target)
    {
        if (target == null)
        {
            Debug.LogError("[CombatVFXSpawner] Hurtbox target is null. Cannot resolve particle spawn position.");
            return null;
        }

        Vector3 spawnPosition = target.transform.position;
        if (target.OwnerRuntime != null)
        {
            spawnPosition = target.OwnerRuntime.WorldPosition;
        }

        return SpawnParticleVFX(prefab, spawnPosition);
    }
}
