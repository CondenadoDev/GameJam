using UnityEngine;

public class RockBehaviour : MonoBehaviour
{
    [SerializeField] float speed;
    void Start()
    {
        speed = 5;   
    }

    // Update is called once per frame
    void FixedUpdate()
    {
       this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y - Time.deltaTime * speed, -1);
    }

    public void SetSpped(float newSpeed)
    {
        speed = newSpeed;
    }

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
