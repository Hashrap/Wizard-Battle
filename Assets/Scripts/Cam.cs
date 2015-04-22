using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Cam : MonoBehaviour
{
	const float CAM_ANGLE = 62;
	const int ZOOM_MIN = 15;
	const int ZOOM_MAX = 40;
	const int SCROLL_SPEED = 100;
	const int SCROLL_AREA = 25;
	
	public GameObject target;
	public float range;
	private Transform myTransform;
	
	private float rad;
	private float zoom = 25;
	private float rX, rY;
	private int width, height, midW, midH;
	private bool observer = false;
		
	void Start() {
		myTransform = transform;
		rad = CAM_ANGLE*Mathf.Deg2Rad;
		width = Screen.width;
		height = Screen.height;
		midW = width/2;
		midH = height/2;
		zoom = ZOOM_MAX;
	}
	
	void Update () {
		if(target != null)
		{
			if(observer)
			{
				double mPosX = Input.mousePosition.x;
				double mPosY = Input.mousePosition.y;
				
				// camera movement by mouse edge panning (rts style)
				if (mPosX < SCROLL_AREA) {myTransform.Translate(Vector3.right * -SCROLL_SPEED * Time.deltaTime);}
				if (mPosX >= Screen.width-SCROLL_AREA) {myTransform.Translate(Vector3.right * SCROLL_SPEED * Time.deltaTime);}
				if (mPosY < SCROLL_AREA) {myTransform.Translate(Vector3.forward * -SCROLL_SPEED * Time.deltaTime, Space.World);}
				if (mPosY >= Screen.height-SCROLL_AREA) {myTransform.Translate(Vector3.forward * SCROLL_SPEED * Time.deltaTime, Space.World);}
			
				// camera movement by keyboard; WASD/Arrows
				myTransform.Translate(new Vector3(Input.GetAxis("Horizontal") * SCROLL_SPEED * Time.deltaTime, 0, Input.GetAxis("Vertical") * SCROLL_SPEED * Time.deltaTime), Space.World);
			}
			else
			{
				//camera movement by mouse ratio panning
				rX = (Input.mousePosition.x-midW)/Screen.width;
				rY = (Input.mousePosition.y-midH)/Screen.height;
				
				//Sets camera at avatar
				myTransform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);
				//Offsets camera according to 1.) Zoom and rotation 2.) Where the mouse is in the target's coordinate space
				Transform tempTransform = target.transform;
				
				GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();				
		
				
				tempTransform.rotation = gm.LocalWizard.GetComponent<Wizard>().defaultRotation;
				
				myTransform.Translate(new Vector3(0.0f, zoom * Mathf.Sin(rad), -zoom * Mathf.Cos(rad)), tempTransform);
				myTransform.Translate(new Vector3(range*rX,0,range*rY), target.transform);
			}
			//Zoom conditional
			if(Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				zoom -= Input.GetAxis("Mouse ScrollWheel")*30;
				if(zoom < ZOOM_MIN)
					zoom = ZOOM_MIN;
				if(zoom > ZOOM_MAX)
					zoom = ZOOM_MAX;
			}
			//Set the transform
			transform.position = myTransform.position;
		}
		else
		{
			GameManager gm = GameManager.Instance;
			if( gm.LocalWizard != null)
				target = gm.LocalWizard;
		}
	}
}