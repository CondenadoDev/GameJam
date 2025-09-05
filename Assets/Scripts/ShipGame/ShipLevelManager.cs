using System.Collections.Generic;
using UnityEngine;

public class ShipLevelManager : MonoBehaviour
{
    [SerializeField] bool canPlay;
    [SerializeField] float seaSpeed;
    [SerializeField] float elapsedSeaSpawnTime;
    [SerializeField] float seaKillPosition;
    [SerializeField] float timeToSpawnRocks;
    [SerializeField] float elapsedRocksSpawnTime;
    [SerializeField] float xOffsetSpawnRocks;
    [SerializeField] float currentShipCount;
    [SerializeField] float maxShipCount;
    [SerializeField] public static ShipLevelManager Instance;
    [SerializeField] Ship playerShip;
    [SerializeField] EnemyShip[] shipCount;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject sea;
    [SerializeField] GameObject enemyShipReference;
    [SerializeField] GameObject seaObstacle;
    [SerializeField] GameObject goal;
    [SerializeField] List<GameObject> seaList;
    [SerializeField] List<RockBehaviour> seaObstaclesList;
    [SerializeField] List<RockBehaviour> currentObstacles;


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        //DEFAULT VALUES
       /* seaSpeed = 5;
        elapsedSeaSpawnTime = 0;
        seaKillPosition = -80;

        timeToSpawnRocks = 1;
        xOffsetSpawnRocks = 6;

        currentShipCount = 0;
        maxShipCount = 3;
        canPlay = true;*/
        seaList.Add(sea);
    }

    private void FixedUpdate()
    {
        elapsedSeaSpawnTime += Time.deltaTime;
        elapsedRocksSpawnTime += Time.deltaTime;

        EnemyShipController();
        SeaBehaviour();
        SpawnObstacles();
    }

    public void EnemyShipController()
    {
        shipCount = Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.None);
        if (shipCount.Length <= 0)
        {
            if (currentShipCount < maxShipCount)
            {
                var s = Instantiate(enemyShipReference, new Vector3(0, -10, -1), Quaternion.identity);
                currentShipCount++;
            }
            else
            {
                if (canPlay)
                {
                    var t = seaList[seaList.Count - 1].transform.position;
                    var g = Instantiate(goal,new Vector3(t.x,t.y,-2), Quaternion.identity);
                    g.transform.parent = seaList[seaList.Count - 1].transform;
                    canPlay = false;

                    /*for(int i = 0; i < seaObstaclesList.Count; i++)
                    {
                        var s = seaObstaclesList[i];
                        seaObstaclesList.Remove(seaObstaclesList[i]);
                        Destroy(s);
                    }*/
                    print("ganaste!");
                }
            }
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
            newSea.transform.parent = this.gameObject.transform;
            seaList.Add(newSea);
            elapsedSeaSpawnTime = 0;
        }
    }

    public void ResetSpeeds(int newSpeed)
    {
        seaSpeed = newSpeed;

        for(int i = 0;i < currentObstacles.Count;i++)
        {
            currentObstacles[i].SetSpped(0);
        }
    }



    public void SpawnObstacles()
    {
        if (elapsedRocksSpawnTime > timeToSpawnRocks && canPlay)
        {
            var newRock = Instantiate(seaObstaclesList[Random.Range(0, seaObstaclesList.Count)], 
                new Vector3(spawnPoint.transform.position.x + (Random.Range(-xOffsetSpawnRocks, xOffsetSpawnRocks)),
                spawnPoint.transform.position.y, -1), Quaternion.identity);
            currentObstacles.Add(newRock);
            //newRock.gameObject.transform.SetParent(seaList[0].gameObject.transform);
            elapsedRocksSpawnTime = 0;
        }
    }

}
