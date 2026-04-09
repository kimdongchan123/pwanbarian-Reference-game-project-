using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Enemy enemyPrefabs;

    public void Start()
    {
        Instantiate(enemyPrefabs);
    }
}
