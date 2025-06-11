using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;



public class ParticleService : NetworkBehaviour
{
    public static ParticleService Instance;

    //* ------------------- Effect Dictionaries --------------- */
    // A queue of particle systems PER particle type.
    public static Dictionary<EffectType, Queue<ParticleSystem>> _pools = new Dictionary<EffectType, Queue<ParticleSystem>>();

    private static Dictionary<EffectType, ParticleSystem> _prefabs = new Dictionary<EffectType, ParticleSystem>();

    [SerializeField] private ParticleServiceLoader loader;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            return;
        }
        foreach (EffectPrefabPair pair in loader.effectPrefabPairs)
        {
            Initialize(pair.type, pair.prefab);
        }
    }

    public static void Initialize(EffectType type, ParticleSystem prefab, int poolSize = 5)
    {
        //if we've already initialized this type, return
        if (_pools.ContainsKey(type)) return;

        //set the corresponding prefab to the type
        _prefabs[type] = prefab;

        //new Particle System queue to be filled. This is the pool we'll pull from for this given type
        Queue<ParticleSystem> q = new Queue<ParticleSystem>();
        for (int i = 0; i < poolSize; i++)
        {
            //Instaniate the particle system, deactivate it, and add it to the queue
            ParticleSystem ps = Instantiate(prefab);
            ps.gameObject.SetActive(false);
            q.Enqueue(ps);
        }

        //set this type's queue to the one we just made
        _pools[type] = q;

    }

    [ClientRpc]
    public void PlayClientRpc(EffectType type, Vector3 position, Quaternion rotation = default)
    {
        if (type == EffectType.None) return;
        //First, try to get an effect from its pool.
        //if the pool hasn't been initialized yet, initialize it.
        if (!_pools.TryGetValue(type, out var q))
        {
            Initialize(type, _prefabs[type]);
        }

        ParticleSystem ps;
        //if theres a particle system waiting for us in the pool, grab it
        if (q.Count > 0)
        {
            ps = q.Dequeue();
        }
        //if not, create a new one
        else
        {
            ps = Instantiate(_prefabs[type]);
        }

        //Set the particle system's position + rotation (rotation will most likely never change)
        ps.transform.SetPositionAndRotation(position, rotation);

        //Stop emitting and clear all particles if it was playing.
        ps.Clear(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        //Set up the main values of our particle system incase we override them
        ParticleSystem.MainModule main = ps.main;

        //Turn on the particle object and play it!
        ps.gameObject.SetActive(true);
        ps.Play();

        //After the particle ends, return it to the queue so we can use it again.
        float lifetime = main.startLifetime.constantMax + main.duration;
        ps.gameObject.GetOrAddComponent<ReturnToPool>().ScheduleReturn(type, lifetime);
    }
}


[System.Serializable]
public enum EffectType
{
    None,
    TreeHit,
    RockHit,
    RockDestroyed,
    StructureHeal
}

