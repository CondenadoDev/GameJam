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
       this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y - Time.deltaTime * 5, -1);
    }

    private void OnBecameInvisible()
    {
        Destroy(this.gameObject);
    }
}
