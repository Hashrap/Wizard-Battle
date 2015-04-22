using System;
using UnityEngine;


public class Knockback: MonoBehaviour
{
	float mass = 3.0f; //mass of character
	Vector3 impact = Vector3.zero; 
	WizardController wizardController;
		
	void Start()
	{	
	}
	
	public void SetUp()
	{
		GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();	
		wizardController = gm.LocalWizardController;
	}
	
	public void AddImpact(Vector3 direction, float force)
	{
		direction.Normalize();
		direction.y = 0;
		impact += direction * force / mass;
	}
	
	void Update()
	{
		if(impact.magnitude > .2)
		{
			this.gameObject.GetComponent<Wizard>().IsBeingKBed = true;
			//character.Move(impact * Time.deltaTime);
		}
		else
			this.gameObject.GetComponent<Wizard>().IsBeingKBed = false;
		
		impact = Vector3.Lerp(impact, Vector3.zero, Time.deltaTime);
		
		if (wizardController!= null){}
			//wizardController.MovementDirty = true;
	}
}