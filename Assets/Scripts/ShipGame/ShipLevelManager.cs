using System.Collections.Generic;
using UnityEngine;

public class ShipLevelManager : MonoBehaviour
{
    [SerializeField] float timeToSpawnSea;
    [SerializeField] float seaSpeed;
    [SerializeField] float elapsedSeaSpawnTime;
    [SerializeField] float seaKillPosition;
    [SerializeField] float timeToSpawnRocks;
    [SerializeField] float elapsedRocksSpawnTime;
    [SerializeField] float xOffsetSpawnRocks;
    [SerializeField] float currentShipCount;
    [SerializeField] float maxShipCount;
    [SerializeField] EnemyShip[] shipCount;
    [SerializeField] List<GameObject> seaList;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject sea;
    [SerializeField] GameObject enemyShipReference;
    [SerializeField] GameObject seaObstacle;
    [SerializeField] List<GameObject> seaObstaclesList;



    private void Start()
    {
        seaSpeed = 5;
        elapsedSeaSpawnTime = 0;
        seaKillPosition = -80;

        timeToSpawnRocks = 1;
        xOffsetSpawnRocks = 6;

        currentShipCount = 0;
        maxShipCount = 3;
        seaList.Add(sea);
    }

    private void FixedUpdate()
    {
        elapsedSeaSpawnTime += Time.deltaTime;
        elapsedRocksSpawnTime += Time.deltaTime;

        ShipController();
        SeaBehaviour();
        SpawnObstacles();
    }

    public void ShipController()
    {
        if (shipCount.Length <= 0)
        {
            if (currentShipCount < maxShipCount)
            {
                Instantiate(enemyShipReference, new Vector3(0, -10, -1), Quaternion.identity);
                currentShipCount++;
            }
            else
                print("ganaste!");
        }
    }

    public void SeaBehaviour()
    {
        for (int i = 0; i < seaList.Count; i++)
        {
            seaList[i].transform.position = new Vector3(seaList[i].transform.position.x, seaList[i].transform.position.y -
                (Time.deltaTime * seaSpeed), seaList[i].transform.position.z);

            if (seaList[i].transform.position.y <= seaKillPosition)
            {
                var s = seaList[i];
                seaList.Remove(seaList[i]);
                sea = seaList[0];
                Destroy(s.gameObject);
            }

        }

        if (elapsedSeaSpawnTime > 10)
        {

            var newSea = Instantiate(sea, spawnPoint.transform.position, Quaternion.identity);
            seaList.Add(newSea);
            elapsedSeaSpawnTime = 0;
        }
    }    

    public void SpawnObstacles()
    {
        if (elapsedRocksSpawnTime > timeToSpawnRocks)
        {
            var newRock = Instantiate(seaObstaclesList[Random.Range(0, seaObstaclesList.Count)], new Vector3(spawnPoint.transform.position.x + (Random.Range(-xOffsetSpawnRocks, xOffsetSpawnRocks)),
                spawnPoint.transform.position.y, -1), Quaternion.identity);

            //newRock.gameObject.transform.SetParent(seaList[0].gameObject.transform);
            elapsedRocksSpawnTime = 0;
        }
    }

}
