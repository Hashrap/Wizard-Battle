using UnityEngine;
using System.Collections;

public class WizardAnimation : MonoBehaviour {
	
	private bool isChecking = false;
	private float checkFrames = 24;
	
	// Use this for initialization
	void Start () {
		animation["throw"].layer = 1;
		//animation["run"].layer = 2;
		//animation["idle_check"].layer = 1;
		animation["idle_breathe"].speed = 0.5f;
		animation.Stop();
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetAxis("Vertical") > 0.2)
			animation.CrossFade("run");
		else
		{
			int chance = Random.Range(0, 100);
			//if(chance > 10 && !isChecking)
			//{
				animation.CrossFade("idle_breathe");
			//}
			//else
			//{
				//animation.CrossFade("idle_check");
				//isChecking = true;
			//}
		}
		
		if(Input.GetMouseButtonDown(0)/* && !GameObject.Find("Main Camera").GetComponent<GameManager>().LocalWizard.GetComponent<Wizard>().fireballOnCD*/)
			animation.CrossFade("throw");
		
		/*
		if(isChecking)
			checkFrames -= 1;
		
		if(checkFrames < 0)
			isChecking = false;*/
		
	}
}
