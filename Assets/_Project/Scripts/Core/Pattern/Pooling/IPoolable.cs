public interface IPoolable
{
    int PrefabID { get; set; }

    void OnSpawn();

    void OnDespawn();
    
    void ReturnToPool();
}