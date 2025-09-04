using System.Collections.Generic;
using UnityEngine;

public class ShipLevelManager : MonoBehaviour
{
    [SerializeField] float timeToSpawnSea;
    [SerializeField] float elapsedSeaSpawnTime;
    [SerializeField] float timeToSpawnRocks;
    [SerializeField] float elapsedRocksSpawnTime;
    [SerializeField] float xOffsetSpawnRocks;
    [SerializeField] EnemyShip[] shipCount;
    [SerializeField] List<GameObject> seaList;
    [SerializeField] GameObject sea;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject seaObstacle;
    [SerializeField] GameObject enemyShipReference;
    [SerializeField] List<Sprite> seaObstaclesList;



    private void Start()
    {
        elapsedSeaSpawnTime = 0;
        timeToSpawnRocks = 3;
        xOffsetSpawnRocks = 8;
        seaList.Add(sea);
    }

    private void FixedUpdate()
    {
        elapsedSeaSpawnTime += Time.deltaTime;
        elapsedRocksSpawnTime += Time.deltaTime;
        shipCount = Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.None);

        if(shipCount.Length <= 0)
        {
            Instantiate(enemyShipReference, new Vector3(0, -6, -1), Quaternion.identity);
        }

        for (int i = 0; i < seaList.Count; i++)
        {
            seaList[i].transform.position = new Vector3(seaList[i].transform.position.x, seaList[i].transform.position.y - 
                (Time.deltaTime * 5), seaList[i].transform.position.z);

            if(seaList[i].transform.position.y <= -45)
            {
                var s = seaList[i];
                seaList.Remove(seaList[i]);
                Destroy(s.gameObject);
            }

        }

        if (elapsedSeaSpawnTime > 10)
        {
            var newSea = Instantiate(sea, spawnPoint.transform.position, Quaternion.identity);
            seaList.Add(newSea);
            elapsedSeaSpawnTime = 0;
        }

        if (elapsedRocksSpawnTime > timeToSpawnRocks)
        {
            var newRock = Instantiate(seaObstacle, new Vector3(spawnPoint.transform.position.x + (Random.Range(-xOffsetSpawnRocks,xOffsetSpawnRocks)), 
                spawnPoint.transform.position.y, -1),Quaternion.identity);


            newRock.gameObject.transform.SetParent(seaList[0].gameObject.transform);

            var sprite = newRock.GetComponent<SpriteRenderer>();

            sprite.sprite = seaObstaclesList[Random.Range(0, seaObstaclesList.Count)];


            elapsedRocksSpawnTime = 0;
        }

    }

}
