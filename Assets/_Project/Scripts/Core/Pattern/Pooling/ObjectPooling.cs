using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooling : SingletonMB<ObjectPooling>
{
    private Dictionary<int, object> pools = new Dictionary<int, object>();
    private Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();
    private HashSet<int> activeInstanceIds = new HashSet<int>();

    private void InitializePool<T>(T instance, int defaultCapacity = 32, int maxSize = 256) where T : Component, IPoolable
    {
        if (instance == null)
        {
            Debug.LogError("[ObjectPooling] Cannot initialize pool from a null prefab.", this);
            return;
        }

        int key = instance.gameObject.GetInstanceID();
        if (pools.ContainsKey(key)) return;

        maxSize = Mathf.Max(1, maxSize);
        defaultCapacity = Mathf.Clamp(defaultCapacity, 1, maxSize);

        GameObject poolParent = new GameObject($"{instance.name}_Pooling");
        poolParent.transform.SetParent(this.transform);
        poolParents[key] = poolParent.transform;

        pools[key] = new ObjectPool<T>(
            createFunc: () =>
            {
                T obj = Instantiate(instance, poolParents[key]);
                obj.PrefabID = key;
                obj.gameObject.SetActive(false);
                return obj;
            },
            actionOnGet: null,
            actionOnRelease: (obj) =>
            {
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(poolParents[key]);
            },
            actionOnDestroy: (obj) => Destroy(obj.gameObject),
            collectionCheck: Debug.isDebugBuild,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    public T Spawn<T>(T instance, Vector3 position, Quaternion rotation, Action<T> onBeforeSpawn = null) where T : Component, IPoolable
    {
        if (instance == null)
        {
            Debug.LogError("[ObjectPooling] Cannot spawn a null prefab.", this);
            return null;
        }

        int key = instance.gameObject.GetInstanceID();

        if (!pools.ContainsKey(key))
        {
            InitializePool(instance);
        }

        if (!TryGetPool(key, instance, out ObjectPool<T> pool))
        {
            return null;
        }

        T obj = pool.Get();

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.PrefabID = key;

        onBeforeSpawn?.Invoke(obj);

        int instanceId = obj.gameObject.GetInstanceID();
        if (!activeInstanceIds.Add(instanceId))
        {
            Debug.LogWarning($"[ObjectPooling] Instance '{obj.name}' is already marked active.", obj);
        }

        obj.gameObject.SetActive(true);
        obj.OnSpawn();

        return obj;
    }

    public void Release<T>(T instance) where T : Component, IPoolable
    {
        if (instance == null)
        {
            Debug.LogWarning("[ObjectPooling] Cannot release a null instance.", this);
            return;
        }

        int key = instance.PrefabID;
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"[ObjectPooling] Cannot find a pool for '{instance.name}'. The GameObject will be destroyed.", instance);
            activeInstanceIds.Remove(instance.gameObject.GetInstanceID());
            instance.OnDespawn();
            Destroy(instance.gameObject);
            return;
        }

        if (!TryGetPool(key, instance, out ObjectPool<T> pool))
        {
            return;
        }

        int instanceId = instance.gameObject.GetInstanceID();
        if (!activeInstanceIds.Remove(instanceId))
        {
            Debug.LogWarning($"[ObjectPooling] Ignored duplicate release for '{instance.name}'.", instance);
            return;
        }

        instance.OnDespawn();
        pool.Release(instance);
    }

    public void Prewarm<T>(T instance, int count, int defaultCapacity = 64, int maxSize = 512) where T : Component, IPoolable
    {
        if (instance == null)
        {
            Debug.LogError("[ObjectPooling] Cannot prewarm a null prefab.", this);
            return;
        }

        if (count <= 0)
        {
            return;
        }

        int key = instance.gameObject.GetInstanceID();
        if (!pools.ContainsKey(key))
        {
            InitializePool(instance, defaultCapacity, maxSize);
        }

        if (!TryGetPool(key, instance, out ObjectPool<T> pool))
        {
            return;
        }

        int targetCount = Mathf.Min(count, Mathf.Max(1, maxSize));
        if (pool.CountInactive >= targetCount)
        {
            return;
        }

        List<T> instances = new List<T>(targetCount);
        for (int i = 0; i < targetCount; i++)
        {
            instances.Add(pool.Get());
        }

        foreach (T pooledInstance in instances)
        {
            pool.Release(pooledInstance);
        }
    }

    private bool TryGetPool<T>(int key, UnityEngine.Object context, out ObjectPool<T> pool) where T : Component, IPoolable
    {
        pool = null;
        if (!pools.TryGetValue(key, out object storedPool))
        {
            return false;
        }

        if (storedPool is ObjectPool<T> typedPool)
        {
            pool = typedPool;
            return true;
        }

        Debug.LogError($"[ObjectPooling] Prefab key {key} was requested with an incompatible component type '{typeof(T).Name}'. " +
                       "Always spawn and release a prefab through the same component type.", context);
        return false;
    }
}

public static class ObjectPoolingHelper
{
    public static T Spawn<T>(T instance, Vector3 position, Quaternion rotation, Action<T> onBeforeSpawn = null) where T : Component, IPoolable
    {
        return ObjectPooling.Instance.Spawn(instance, position, rotation, onBeforeSpawn);
    }

    public static void Release<T>(T instance) where T : Component, IPoolable
    {
        ObjectPooling.Instance.Release(instance);
    }

    public static void Prewarm<T>(T instance, int count, int defaultCapacity = 64, int maxSize = 512) where T : Component, IPoolable
    {
        ObjectPooling.Instance.Prewarm(instance, count, defaultCapacity, maxSize);
    }
}
