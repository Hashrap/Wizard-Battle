using UnityEngine;
//including some .NET for dynamic arrays called List in C#

public class Obstacle : MonoBehaviour {
	
	//Destroy fireballs
	void OnTriggerEnter(Collider collider) 
	{
		if(collider.gameObject.tag.Equals("Fireball"))
			Destroy(collider.gameObject);
    }
}