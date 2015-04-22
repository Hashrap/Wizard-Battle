using UnityEngine;
using System.Collections;
//including some .NET for dynamic arrays called List in C#
using System.Collections.Generic;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;

public class Wizard : MonoBehaviour {
	//default rotation
	public Quaternion defaultRotation;
	
	//Prefabs
	public Object fireballPrefab;
	
	//Stats
	private float health, instability;
	public float Health { get { return health; } set { health = value; } }
	public float Instability { get { return instability; } set { instability = value; } }	
	
	private bool isDead = false;
	public bool IsDead { get { return isDead; } set { isDead = value; } }
	private bool isBeingKBed = false;
	public bool IsBeingKBed { get { return isBeingKBed; } set { isBeingKBed = value; } }
	private bool isOnStage = true;
	public bool IsOnStage { get { return isOnStage; } set { isOnStage = value; } }
	
	//Dangerous stage damage cooldown (it ticks every half second)
	const float STAGE_DAMAGE_COOLDOWN = .5f;
	const float STAGE_DAMAGE = 5;
	private float stageDamageTimer = 0;
	
	//Fireball attributes
	const float FIREBALL_COOLDOWN = 1.5f;
	private float fireballTimer = 0;
	//gui timers
	
	
	//Teleport attributes
	const float TELEPORT_RANGE = 45.0f;
	const float TELEPORT_COOLDOWN = 12.0f;
	private float teleportTimer = 0.0f;
	public GameObject teleportInEffect = null;
	public GameObject teleportOutEffect = null;
	
	//---------------------------------------------------
	//gui timers
	public float teleportGUICD = TELEPORT_COOLDOWN;
	public bool teleportOnCD = false;
	
	public float fireballGUICD = FIREBALL_COOLDOWN;
	public bool fireballOnCD = false;
	
	public float reflectGUICD = 0;
	public bool reflectOnCD = false;
	//----------------------------------------------------------------
	
	private Vector3 pos, size;
	public Texture2D backTex, hFillTex, iFillTex;
	
	private bool isMe = false;
	
	public bool IsMe{ 
		get {return isMe;} 
		set {isMe = value;}}
	
	private User smartFoxUser;
	public User SmartFoxUser{ 
		get {return smartFoxUser;} 
		set {smartFoxUser = value;}}
	
	private Color bodyColor = new Color(0.6f,0.6f,0.6f);
	public Color BodyColor{ 
		get {return bodyColor;} 
		set {
			bodyColor = value;
			//find my capsule and color it this color 
			//Transform bod = transform.Find("Body"); 
			//bod.renderer.material.color = new Color(0, 0, 0);// bodyColor;
		}
	}	
	
	// Use this for initialization
	void Start () {
		health = 100;
		instability = 0;
		fireballTimer = 0;
		isDead = false;
		//-----------------------------------------------
		fireballTimer = FIREBALL_COOLDOWN;
		teleportTimer = TELEPORT_COOLDOWN;
		fireballGUICD= FIREBALL_COOLDOWN;
		teleportGUICD=TELEPORT_COOLDOWN;
		//-----------------------------------------------------
		defaultRotation = this.transform.rotation; // Quaternion.AngleAxis(Mathf.Rad2Deg*(rad*whichCol)-90.0f,Vector3.up);
	}
	
	void Update () {		
		fireballTimer += Time.deltaTime;
		teleportTimer += Time.deltaTime;
		stageDamageTimer += Time.deltaTime;
		
		//----------------------------------------------------------------------------------------------------------------
		if(fireballOnCD)
			fireballGUICD -= Time.deltaTime;
		if(teleportOnCD)
			teleportGUICD -= Time.deltaTime;
		//----------------------------------------------------------------------------------------------------------------
		
		//lose health out of stage
		if(isMe && !isOnStage && stageDamageTimer >= STAGE_DAMAGE_COOLDOWN)
		{
			stageDamageTimer = 0;
			ChangeHealthInstability(STAGE_DAMAGE);
		}
		
		//kill wizard if <0 hp
		if(!isDead && health<=0)
		{
			health = 0;
			KillWizard();
		}
		
		Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		RaycastHit hit = new RaycastHit();			
		if(Physics.Raycast(ray, out hit))
		{	
			//rotate towards cursor
			if(!isDead && isMe) { this.transform.LookAt(new Vector3(hit.point.x, this.transform.position.y, hit.point.z)); } 
			
			//shoot fireball
			if(isMe && !isDead && Input.GetMouseButton (0) && fireballTimer >= FIREBALL_COOLDOWN)
			{
				fireballTimer = 0;
				//----------------------------------------------------------------------------------------------------------------
				fireballGUICD = FIREBALL_COOLDOWN;
				fireballOnCD = true;
				//----------------------------------------------------------------------------------------------------------------
				
				Fireball fb;
				//Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				//RaycastHit hit = new RaycastHit();
				
				if(Physics.Raycast(ray, out hit))
				{	
					
					//Spawn fireball
					Vector3 spawnPoint = this.transform.position;
					spawnPoint.y+=2;
					
					fb = ((GameObject)Instantiate(fireballPrefab, spawnPoint, Quaternion.identity)).GetComponent<Fireball>();
					fb.Owner = this.gameObject;
					Vector3 targetPoint = hit.point - this.transform.position; 
					targetPoint.y = 0; //spawnPoint.y; 
					fb.transform.forward = targetPoint;
					
					
					Debug.Log(hit.point.ToString());
					
					//***Send fireball data***
					GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();				
					
					SFSObject fbData = new SFSObject();		
					
					fbData.PutUtfString("type", "fireball");
					fbData.PutFloat("positionX", fb.transform.position.x);
					fbData.PutFloat("positionY", fb.transform.position.y);
					fbData.PutFloat("positionZ", fb.transform.position.z);
					fbData.PutFloat("forwardX", fb.transform.forward.x);
					fbData.PutFloat("forwardY", fb.transform.forward.y);
					fbData.PutFloat("forwardZ", fb.transform.forward.z);
					gm.sendDataObj(fbData);
				}
			}
			
			//Teleport
			if(isMe && !isDead && Input.GetKeyDown(KeyCode.Space) && teleportTimer >= TELEPORT_COOLDOWN && !isDead )
			{
				teleportTimer = 0;
				//----------------------------------------------------------------------------------------------------------------
				teleportGUICD=TELEPORT_COOLDOWN;
				teleportOnCD=true;
				//----------------------------------------------------------------------------------------------------------------
				if(Physics.Raycast(ray, out hit))
				{
					Vector3 targetPoint = hit.point - this.transform.position; 					
					targetPoint.y = 0;
					
					
					if(targetPoint.magnitude > TELEPORT_RANGE)
					{
						targetPoint.Normalize();
						targetPoint *= TELEPORT_RANGE;
					}
					
					Instantiate(teleportOutEffect, this.transform.position, this.transform.rotation);
					
					this.transform.Translate(targetPoint, Space.World);
					
					Instantiate(teleportInEffect, this.transform.position, this.transform.rotation);
					
					//Send movement data
					GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();
					SFSObject transformData = new SFSObject();						
					transformData.PutUtfString("type", "teleport");
					transformData.PutFloat("positionX", transform.position.x);
					transformData.PutFloat("positionY", transform.position.y);
					transformData.PutFloat("positionZ", transform.position.z);
					transformData.PutFloat("rot", transform.rotation.eulerAngles.y);
					gm.sendDataObj(transformData);					
				}
			}
		}
		
		//----------------------------------------------------------------------------------------------------------------
		//reset cooldown for abilities
		if(fireballTimer >= FIREBALL_COOLDOWN && fireballOnCD)
			fireballOnCD=false;
		if(teleportTimer >= TELEPORT_COOLDOWN && teleportOnCD)
			teleportOnCD=false;
		/*if(reflect is off cd)
			reflectOnCD=false;
		*/
		//----------------------------------------------------------------------------------------------------------------
		//Check if outside stage		
		float distFromStage = Mathf.Sqrt(Mathf.Pow(this.transform.position.x, 2.0f) + Mathf.Pow(this.transform.position.z, 2.0f));
		float stageScale = GameObject.Find("SafeTerrain").transform.lossyScale.x/2;
		if(distFromStage > stageScale && isOnStage)
			isOnStage = false;
		else if(distFromStage <= stageScale && !isOnStage)
			isOnStage = true;
	}
	
	// GUIUpdate is called once per frame
	void OnGUI() {
		if(!IsMe)
		{
			pos = Camera.main.WorldToScreenPoint(transform.position);
			GUI.BeginGroup(new Rect(pos.x-(backTex.width/2), (-1*pos.y) + (Screen.height-50), backTex.width, backTex.height), backTex);
			 GUI.BeginGroup(new Rect(4,3,(Health/100.0f)*(hFillTex.width), hFillTex.height));
			  GUI.DrawTexture(new Rect(0,0,hFillTex.width, hFillTex.height), hFillTex, ScaleMode.StretchToFill);
			 GUI.EndGroup();
			 GUI.BeginGroup(new Rect(4,14,(Instability/100.0f)*iFillTex.width, iFillTex.height));
			  GUI.DrawTexture(new Rect(0,0,iFillTex.width, iFillTex.height), iFillTex, ScaleMode.StretchToFill);
			 GUI.EndGroup();
	        GUI.EndGroup();
		}
	}
	
	
	void OnTriggerEnter(Collider collider) 
	{
		string colliderTag = collider.gameObject.tag;
		//if hit by fireball
		if(!isDead && colliderTag.Equals("Fireball"))
		{			
			Fireball fb = collider.gameObject.GetComponent<Fireball>();
			if(fb.Owner != this.gameObject && isMe)
			{
				Debug.Log("On fb trigger fb.Owner:" + fb.Owner + " and this.Gameobject = " +this.gameObject);
				this.gameObject.GetComponent<WizardController>().AddImpact(fb.transform.forward, fb.Velocity * (100f + instability) *.01f);
				//Knockback kbScript = this.GetComponent<Knockback>();
      			//kbScript.AddImpact(fb.transform.forward, fb.Velocity * (100f + instability) *.01f); //multiply by wizard's instability
				isBeingKBed = true;
				ChangeHealthInstability(fb.Damage, fb.Instability);
				Destroy(fb.gameObject);
				Debug.Log("fireball/opponent wizard collision; Force: "+(fb.Velocity*(100+instability)*.01));
			}
		}
    }	
	
	void OnTriggerExit(Collider collider) 
	{
		string colliderTag = collider.gameObject.tag;		
		if(!isDead && colliderTag.Equals("Stage"))
			isOnStage = false;
    }
	
	// SUBTRACTS HEALTH, ADDS INSTABILITY
	public void ChangeHealthInstability(float h, float i = 0)
	{
		if(!isDead)
		{
			health -= h;
			instability += i;
			
			//Player is dead
			if(health<=0)
			{
				health = 0;
				KillWizard();
			}
			
			//Send stat data
			GameManager gm = GameObject.Find("Main Camera").GetComponent<GameManager>();				
			
			SFSObject statData = new SFSObject();		
			
			statData.PutUtfString("type", "stats");
			statData.PutFloat("health", health);
			statData.PutFloat("instability", instability);
			gm.sendDataObj(statData);
		}
	}
	
	//Remove wizard from game
	private void KillWizard()
	{
		this.gameObject.transform.Rotate(0f, 0f, 90f);
		this.gameObject.gameObject.transform.Rotate(0.0f, 0.0f, 90.0f);
		this.gameObject.active = false;
		isDead = true;
		
		//Scoreboard/win condition called here
		
		//scoreboard.player[name].deaths++
		//gamemanager.CheckWin();
	}
}
