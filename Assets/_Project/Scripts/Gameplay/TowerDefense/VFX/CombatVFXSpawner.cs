using UnityEngine;

public static class CombatVFXSpawner
{
    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Vector3 position, CombatTimeController combatTime)
    {
        return SpawnSimpleSpriteVFX(prefab, position, Quaternion.identity, combatTime);
    }

    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Vector3 position, Quaternion rotation,
                                                                CombatTimeController combatTime)
    {
        if (prefab == null || combatTime == null)
        {
            Debug.LogError("[CombatVFXSpawner] SimpleSpriteAnimatorVFX prefab and CombatTimeController are required to spawn VFX.", prefab);
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, position, rotation, spawnedVFX => spawnedVFX.SetCombatTime(combatTime));
    }

    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Hurtbox target, CombatTimeController combatTime)
    {
        return SpawnSimpleSpriteVFX(prefab, target, Quaternion.identity, combatTime);
    }

    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Hurtbox target, Quaternion rotation, CombatTimeController combatTime)
    {
        if (prefab == null || target == null)
        {
            return null;
        }

        Transform anchor = target.transform;
        if (target.OwnerRuntime != null && target.OwnerRuntime.VFXAnchor != null)
        {
            anchor = target.OwnerRuntime.VFXAnchor;
        }

        return SpawnSimpleSpriteVFX(prefab, anchor, rotation, combatTime);
    }

    public static SimpleSpriteAnimatorVFX SpawnSimpleSpriteVFX(SimpleSpriteAnimatorVFX prefab, Transform anchor, Quaternion rotation, CombatTimeController combatTime)
    {
        if (prefab == null || anchor == null || combatTime == null)
        {
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, anchor.position, rotation, spawnedVFX =>
        {
            spawnedVFX.SetCombatTime(combatTime);
            spawnedVFX.transform.SetParent(anchor, true);
            spawnedVFX.transform.localPosition = Vector3.zero;
        });
    }

    public static TriggeredSpriteAnimatorVFX SpawnTriggeredSpriteVFX(TriggeredSpriteAnimatorVFX prefab, Transform anchor, Quaternion rotation, CombatTimeController combatTime)
    {
        if (prefab == null || anchor == null || combatTime == null)
        {
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, anchor.position, rotation, spawnedVFX =>
        {
            spawnedVFX.SetCombatTime(combatTime);
            spawnedVFX.transform.SetParent(anchor, true);
            spawnedVFX.transform.localPosition = Vector3.zero;
        });
    }

    public static TriggeredSpriteAnimatorVFX SpawnTriggeredSpriteVFX(TriggeredSpriteAnimatorVFX prefab, Vector3 position, Quaternion rotation,
                                                                      CombatTimeController combatTime)
    {
        if (prefab == null || combatTime == null)
        {
            Debug.LogError("[CombatVFXSpawner] TriggeredSpriteAnimatorVFX prefab and CombatTimeController are required to spawn VFX.", prefab);
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, position, rotation, spawnedVFX => spawnedVFX.SetCombatTime(combatTime));
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Vector3 position, CombatTimeController combatTime)
    {
        return SpawnParticleVFX(prefab, position, Quaternion.identity, combatTime);
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Vector3 position, Quaternion rotation, CombatTimeController combatTime)
    {
        if (prefab == null || combatTime == null)
        {
            Debug.LogError("[CombatVFXSpawner] ParticleVFX prefab and CombatTimeController are required to spawn VFX.", prefab);
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, position, rotation, spawnedVFX => spawnedVFX.SetCombatTime(combatTime));
    }

    public static ParticleVFX SpawnParticleVFX(ParticleVFX prefab, Hurtbox target, CombatTimeController combatTime)
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

        return SpawnParticleVFX(prefab, spawnPosition, combatTime);
    }

    public static LoopingStatusVFX SpawnLoopingStatusVFX(LoopingStatusVFX prefab, Transform anchor, CombatTimeController combatTime)
    {
        if (prefab == null || anchor == null || combatTime == null)
        {
            return null;
        }

        return ObjectPoolingHelper.Spawn(prefab, anchor.position, anchor.rotation, spawnedVFX =>
        {
            spawnedVFX.SetCombatTime(combatTime);
            spawnedVFX.transform.SetParent(anchor);
            spawnedVFX.transform.localPosition = Vector3.zero;
            spawnedVFX.transform.localRotation = Quaternion.identity;
        });
    }
}
