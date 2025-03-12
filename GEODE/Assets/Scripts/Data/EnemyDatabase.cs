using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemies/EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    private static EnemyDatabase instance;
    [SerializeField] private List<GameObject> enemies;
    private Dictionary<int, GameObject> enemyDictionary = new Dictionary<int, GameObject>();

    public static EnemyDatabase Instance
    {
        get => instance;
    }

    private void OnEnable()
    {
        if(instance == null)
        {
            instance = this;
            BuildDictionary();
        }
        else
        {
            Debug.Log("ERROR. MULTIPLE ENEMYDATABASEs");
        }
    }

    private void BuildDictionary()
    {
        foreach(GameObject enemy in enemies )
        {
            enemyDictionary.Add(enemy.GetComponent<BaseEnemy>().Id, enemy);
        }
    }

    public GameObject GetEnemy(int id)
    {
        if(enemyDictionary.ContainsKey(id))
        {
            return enemyDictionary[id];
        }
        return null;
        
    }
    

    

   
}
