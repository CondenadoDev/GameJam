using UnityEngine;
using UnityEngine.UIElements;

public class ShipBullet : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] int dmg;
    [SerializeField] float lifeTime;
    [SerializeField] private EnemyShip parent;
    void Start()
    {
        speed = 0.1f;
        dmg = 1;
        lifeTime = 5;
    }

    public void GetEnemyShipParent(EnemyShip p)
    {
        parent = p;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + speed, this.transform.position.z);
    
        lifeTime -= Time.deltaTime;

        if(lifeTime < 0 )
        {
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.GetComponent<Ship>() != null)
        {
            var ship = collision.gameObject.GetComponent<Ship>();

            ship.GetDamage(dmg);
            if (ship.GetHealth() <= 0)
            {
                this.gameObject.SetActive(false);
                if (ship.GetHealth() <= 0)
                {
                    parent.canShoot = false;
                }
            }
        }
        Destroy( this.gameObject );
    }
}
