using UnityEngine;
using System.Collections;

public class WizardController : MonoBehaviour {
	
	float mass = 3.0f; //mass of character
	Vector3 impact = Vector3.zero; 
	WizardController wizardController;
	
	public float movementSpeed = 15;
	public GameObject relativePosition;
	
	// Dirty flag for checking if movement was made or not
	public bool MovementDirty {get; set;}

	void Start() {
		MovementDirty = false;
		//relativePosition = new GameObject();		
		relativePosition = GameObject.FindGameObjectWithTag("RelativeObject"); //GameObject.CreatePrimitive(PrimitiveType.Sphere);
	
		GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();	
		wizardController = gm.LocalWizardController;
	}
	
	public void SetRots()
	{
		//relativePosition.transform.position = Camera.main.transform.position;
		//relativePosition.transform.rotation.Set(0, Camera.main.transform.rotation.y, 0, 0);
		//Debug.Log("transform rotation: " + relativePosition.transform.rotation);
	}
	
	public void AddImpact(Vector3 direction, float force)
	{
		direction.Normalize();
		direction.y = 0;
		impact += direction * force / mass;
	}
	
	void Update () {
		// Forward/backward makes player model move				
		float verticalTranslation = Input.GetAxis("Vertical");
		float horizontalTranslation = Input.GetAxis("Horizontal");	
		
		//If we're moving both vertically and horizontally
		if (verticalTranslation != 0 && horizontalTranslation != 0) {
			this.transform.Translate(horizontalTranslation * Time.deltaTime * movementSpeed * (1.0f/Mathf.Sqrt(2.0f)), 0, verticalTranslation * Time.deltaTime * movementSpeed * (1.0f/Mathf.Sqrt(2.0f)), relativePosition.transform);
			MovementDirty = true;
		}	
		//if we're moving only vertically
		else if (verticalTranslation != 0) {
			this.transform.Translate(0, 0, verticalTranslation * Time.deltaTime * movementSpeed, relativePosition.transform);
			MovementDirty = true;
		}	
		//if we're moving only horizontally
		else if (horizontalTranslation != 0) {
			this.transform.Translate(horizontalTranslation * Time.deltaTime * movementSpeed, 0, 0,  relativePosition.transform);
			MovementDirty = true;
		}
		
		//Knocked back?
		if(impact.magnitude > .2)
		{
			this.gameObject.GetComponent<Wizard>().IsBeingKBed = true;
			//character.Move(impact * Time.deltaTime);
			this.gameObject.transform.Translate(impact * Time.deltaTime, Space.World);
			MovementDirty = true;
		}
		else
			this.gameObject.GetComponent<Wizard>().IsBeingKBed = false;
		
		impact = Vector3.Lerp(impact, Vector3.zero, Time.deltaTime);
	}
}
