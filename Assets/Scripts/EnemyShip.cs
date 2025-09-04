using System;
using UnityEngine;

public class EnemyShip : MonoBehaviour
{
    [SerializeField] private Ship targetShip;
    [SerializeField] private float frecuencyAcceleration;
    [SerializeField] private float initialAcceleration;
    [SerializeField] private float currentAcceleration;
    [SerializeField] private float countUp;
    [SerializeField] private float bulletSpawnTimer;
    [SerializeField] private float currentBulletTimer;
    [SerializeField] private ShipBullet bullet;
    [SerializeField] public bool canShoot;
    void Start()
    {
        try
        {
            targetShip = FindFirstObjectByType<Ship>();
            Debug.Log("Ship found!");
        }
        catch (Exception ex)
        {
            Debug.Log("There is no ship");
        }
        currentAcceleration = 0;
        initialAcceleration = 0.01f;
        countUp = 0;
        frecuencyAcceleration = 0.3f;
        bulletSpawnTimer = 2;
        currentBulletTimer = 0;
        canShoot = true;
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            countUp += Time.deltaTime;
            if (countUp > frecuencyAcceleration)
            {
                SetAcceleration();
                countUp = 0;
            }
            this.transform.position = new Vector3(targetShip.transform.position.x, this.transform.position.y, this.transform.position.z);


            if(currentBulletTimer >= bulletSpawnTimer && canShoot)
            {
                Instantiate(bullet, new Vector3 (this.transform.position.x, this.gameObject.transform.position.y + 1.5f, 
                    this.gameObject.transform.position.z), Quaternion.identity);

                bullet.GetEnemyShipParent(this);
                currentBulletTimer = 0;
            }else
            {
                currentBulletTimer += Time.deltaTime;
            }
        }
        catch (Exception ex)
        {
            print("no ship found");
            canShoot = false;
        }
    }

    public void SetAcceleration()
    {
        try
        {
            this.transform.position = new Vector3(targetShip.transform.position.x, this.transform.position.y + currentAcceleration, this.transform.position.z);
            currentAcceleration += initialAcceleration;
        }
        catch (Exception ex) 
        { 
            Debug.Log("No ship found");
            canShoot = false;
        }
        
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Ship>() != null)
        {
            Destroy(collision.gameObject);
        }else if(collision.gameObject.GetComponent<RockBehaviour>() != null)
        {
            Destroy(this.gameObject);
        }
    }



}
