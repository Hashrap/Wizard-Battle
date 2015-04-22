using UnityEngine;
using System.Collections;

public class Fireball : MonoBehaviour {
	
	const double MAX_DISTANCE_TRAVELED = 100;
	
	//ALWAYS POSITIVE
	private float damage = 15f;
	public float Damage { get { return damage; } }
	
	//ALWAYS POSITIVE
	private float instability = 25f;
	public float Instability { get { return instability; } }
	
	
	private float velocity = 60f;
	public float Velocity { get { return velocity; } }
	
	
	private double distanceTraveled;
	public double DistanceTraveled { get {return distanceTraveled; } }	
	
	private Vector3 moveDirection;
	
	private GameObject owner;
	public GameObject Owner { get { return owner; } set { owner = value; } }
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {		
		//move fireball
		if(distanceTraveled > MAX_DISTANCE_TRAVELED)
		{
			Destroy (this.gameObject);
			//Debug.Log("Fireball range limit reached");
		}
		Vector3 t = transform.forward * (velocity * Time.deltaTime);
		distanceTraveled += t.magnitude;
		this.transform.position += t;		
	}
	
	 void OnCollisionEnter(Collision collision)
	{
		Debug.Log("collision");
		if(collision.gameObject.tag.Equals("Obstacle"))
		{
			
		}
	}
}
