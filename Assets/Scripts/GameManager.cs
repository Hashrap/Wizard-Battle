using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Sfs2X.Exceptions;

public class GameManager : MonoBehaviour {
	//Prefabs
	public GameObject avatarPrefab;		// to instatiate player character
	public GameObject characterPrefab;	// to instantiate other people's characters
	public GameObject fireballPrefab;
	public GameObject teleportOutEffect;
	public GameObject teleportInEffect;
	
	//time manager stuff
	public readonly static string ExtName = "sfsFps";
	public readonly static string ExtClass ="dk.fullcontrol.fps.FpsExtension";
	
	private SmartFox smartFox;          //Smartfox object
	
	private Lobby lobby;                //Lobby
	
	private Room currentRoom;           //Room
	
	private bool gameOn = false;        //is the game running
	
  	private GameObject localWizard;  	//local wizard - includes scripts for user input, movement, camera following, etc.	
	public GameObject LocalWizard { get { return localWizard; } }
	
	private WizardController localWizardController;
	public WizardController LocalWizardController { get { return localWizardController; } set { value = localWizardController; } }
	
	private Dictionary<User, GameObject> remoteWizards;	//List of other wizards
	private List<Color> colors;		    // to provide a different appearance for each character
	private List<Vector3> positions;    // arbitrary initial spawn positions; not a great solution (make it better!)
	private float rad;					//short for Mathf.PI*2/currentRoom.UserCount;
			
	public Texture2D healthBar;         //GUI objects
	public Texture2D[] icons;
	public GUISkin hudSkin;

	private string clientName;
	public string ClientName { get { return clientName;}	}
	
	private static GameManager instance;
	public static GameManager Instance { get { return instance;}	}
	
	void Awake() {	instance = this;	}
	
	void Start () {
		bool debug = true;
		gameOn = true;
		if (SmartFoxConnection.IsInitialized)
			smartFox = SmartFoxConnection.Connection;
		else
			smartFox = new SmartFox(debug);
		
		currentRoom = smartFox.LastJoinedRoom;
		clientName = smartFox.MySelf.Name;
		
		TimeManager.Instance.Init();
		
		int size = currentRoom.GetVariable("mapSize").GetIntValue();
		float length = (float)currentRoom.GetVariable("roundLength").GetIntValue();
		GameObject stage = GameObject.Find("SafeTerrain");
		SafeAreaCylinder sac = stage.GetComponent<SafeAreaCylinder>();
		sac.set_stage(size, length);
		
		// set up arrays of colors and spawn positions for various players
		colors = new List<Color>(){Color.white, Color.red, Color.yellow, Color.green, Color.blue};
		positions = new List<Vector3>();
		rad = Mathf.PI*2/currentRoom.UserCount;
		for (int i = 0; i < currentRoom.UserCount; i++){
			Vector3 temp = new Vector3(sac.originalStageScale*Mathf.Cos(rad*i)/2.25f,3,sac.originalStageScale*Mathf.Sin(rad*i)/2.25f);
			positions.Add(temp);
			//Debug.Log(temp.ToString());
		}
		remoteWizards = new Dictionary<User, GameObject>();
		
		// create my avatar
		MakeWizard(smartFox.MySelf);
		//make camera follow the avatar
		Camera.main.GetComponent<Cam>().target = localWizard;
		Camera.main.transform.RotateAround(localWizard.transform.position, Vector3.up, localWizard.transform.parent.localEulerAngles.y);
		localWizardController.SetRots();
		SubscribeDelegates();
	}
	
	void SubscribeDelegates(){
		// listen for smartfox events 
		smartFox.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
		smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
		smartFox.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChange);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		smartFox.AddEventListener(SFSEvent.SPECTATOR_TO_PLAYER, OnSpectatorToPlayer);
		smartFox.AddEventListener(SFSEvent.SPECTATOR_TO_PLAYER_ERROR, OnSpectatorToPlayerError);
		smartFox.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessageReceived);
		smartFox.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVariablesUpdate); 
		smartFox.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
	}
	
	// Request the current server time. Used for time synchronization
	public void TimeSyncRequest() 
	{
			ExtensionRequest request = new ExtensionRequest("getTime", new SFSObject(), currentRoom);
			smartFox.Send(request);
	}
	private void OnExtensionResponse(BaseEvent evt) {
			try {
						string cmd = (string)evt.Params["cmd"];
					ISFSObject dt = (SFSObject)evt.Params["params"];
			if (cmd == "time") 
			{
						HandleServerTime(dt);
			}
			}
			catch (Exception e) 
			{
				Debug.Log("Exception handling response: "+e.Message+" >>>"+e.StackTrace);
			}
	}
	// Synchronizes time from the server //
	private void HandleServerTime(ISFSObject dt)
	{
		long time = dt.GetLong("t");
		TimeManager.Instance.Synchronize(Convert.ToDouble(time));
	}
	void FixedUpdate() {
		
		if (smartFox != null) {
			smartFox.ProcessEvents();		
			
			
			// If we spawned a local player, send position if movement is dirty
			if (localWizard != null && localWizardController != null && localWizardController.MovementDirty) {
				
				SFSObject transformData = new SFSObject();						
				transformData.PutUtfString("type", "transform");
				transformData.PutFloat("positionX", localWizard.transform.position.x);
				transformData.PutFloat("positionY", localWizard.transform.position.y);
				transformData.PutFloat("positionZ", localWizard.transform.position.z);
				transformData.PutFloat("rot", localWizard.transform.rotation.eulerAngles.y);
				smartFox.Send(new ObjectMessageRequest(transformData));
				
				localWizardController.MovementDirty = false;
			}
		}			
	}
	
	void MakeWizard (User user)
	{
		int whichCol = GetColorNumber (user);
		Debug.Log (whichCol.ToString());
		GameObject wiz;
		
		//If this is me
		if (user.IsItMe) 
		{ 
			wiz = Instantiate (avatarPrefab, positions[whichCol], Quaternion.AngleAxis(Mathf.Rad2Deg*(rad*whichCol)-90.0f,Vector3.up)) as GameObject;
			wiz = wiz.transform.FindChild("AvatarPF").gameObject;
			localWizard = wiz;
			localWizard.GetComponent<Wizard>().defaultRotation = Quaternion.AngleAxis(Mathf.Rad2Deg*(rad*whichCol)-90.0f,Vector3.up);
			
			//add the wizardcontroller
			localWizard.AddComponent<WizardController>();
			localWizardController = localWizard.GetComponent<WizardController>();
			
		}
		//If this is another playa
		else 
		{
			wiz = Instantiate (characterPrefab, positions[whichCol], Quaternion.identity) as GameObject;
			GameObject remoteWizard = wiz;
			remoteWizard.AddComponent<SimpleRemoteInterpolation>();
			remoteWizard.GetComponent<SimpleRemoteInterpolation>().SetTransform(positions[whichCol], Quaternion.identity, false);		
			
			remoteWizards.Add(user, remoteWizard);
		}			
		
		//Debug.Log("before setup");
		wiz.GetComponent<Knockback>().SetUp();
		//Debug.Log("after setup");
		Wizard wz = wiz.GetComponent<Wizard>();
		wz.BodyColor = colors[whichCol];
		wz.IsMe = user.IsItMe; 
		wz.SmartFoxUser = user;
	}
	
	public void sendDataObj(SFSObject dataToSend)
	{						
		smartFox.Send(new ObjectMessageRequest(dataToSend));
	}
	
	private int GetColorNumber (User user){
		int colorNum = -1;

		if (user.IsItMe) {
			//assign a new number
			//first get a copy of available numbers, which is a room variable
			SFSArray numbers = (SFSArray)currentRoom.GetVariable ("colorNums").GetSFSArrayValue ();
			colorNum = numbers.GetInt (0);
			Debug.Log("colorIndex: "+colorNum);
			//update room variable 
			numbers.RemoveElementAt (0);
			//send back to store on server
			List<RoomVariable> rData = new List<RoomVariable> ();
			rData.Add (new SFSRoomVariable ("colorNums", numbers));
			smartFox.Send (new SetRoomVariablesRequest (rData));
			
			//store my own color on server as user data
			List<UserVariable> uData = new List<UserVariable> ();
			uData.Add (new SFSUserVariable ("colorIndex", colorNum));
			smartFox.Send (new SetUserVariablesRequest (uData));
		
		} else {
			try {
				colorNum = (int)user.GetVariable ("colorIndex").GetIntValue( );
			}
			catch (Exception ex) {
				Debug.Log ("error in else of getColorNumber "+ex.ToString());
			}
		}	
		return colorNum;
	}
	
	// when user enters room, we send our transform so they can update us
	public void OnUserEnterRoom (BaseEvent evt){
		// User joined - and we might be standing still (not sending position info). So lets send him our position info
		if (localWizard != null) {/*
			List<UserVariable> userVariables = new List<UserVariable>();
			userVariables.Add(new SFSUserVariable("x", (double)localWizard.transform.position.x));
			userVariables.Add(new SFSUserVariable("y", (double)localWizard.transform.position.y));
			userVariables.Add(new SFSUserVariable("z", (double)localWizard.transform.position.z));
			userVariables.Add(new SFSUserVariable("rot", (double)localWizard.transform.rotation.eulerAngles.y));
			smartFox.Send(new SetUserVariablesRequest(userVariables));*/
			SFSObject transformData = new SFSObject();						
			transformData.PutUtfString("type", "transform");
			transformData.PutFloat("positionX", localWizard.transform.position.x);
			transformData.PutFloat("positionY", localWizard.transform.position.y);
			transformData.PutFloat("positionZ", localWizard.transform.position.z);
			transformData.PutFloat("rot", localWizard.transform.rotation.eulerAngles.y);
			smartFox.Send(new ObjectMessageRequest(transformData));
		}
	}

	private void OnUserLeaveRoom(BaseEvent evt) {
		//remove this user from our world and update our data structures
		User user = (User)evt.Params["user"];
		int colorNum = (int)user.GetVariable("colorIndex").GetIntValue();
		SFSArray numbers = (SFSArray)currentRoom.GetVariable("colorNums").GetSFSArrayValue();
		
		//update room variable 
		numbers.AddInt(colorNum); 
		//send back to store on server
		List<RoomVariable> rData = new List<RoomVariable>();
		rData.Add(new SFSRoomVariable("colorNums", numbers));
		smartFox.Send(new SetRoomVariablesRequest(rData));
		
		//remove the other's gameobject
		Destroy(remoteWizards[user]);
		remoteWizards.Remove(user);
	}

	
	public void OnUserCountChange(BaseEvent evt) {
		Debug.Log("Game room user count changed");
	}
	
	// When connection is lost we load the login scene
	private void OnConnectionLost(BaseEvent evt) {
		UnsubscribeDelegates();
		Screen.lockCursor = false;
		Screen.showCursor = true;
		Application.LoadLevel("The Lobby");
	}
	private void OnSpectatorToPlayer (BaseEvent evt){
			User user = (User)evt.Params["user"];
	}
	
	private void OnSpectatorToPlayerError (BaseEvent evt){
		User user = (User)evt.Params["user"];

	}
	private void UnsubscribeDelegates() {
		smartFox.RemoveAllEventListeners();
	}
	void OnApplicationQuit() {
		UnsubscribeDelegates();
	}
	
	// When user variable is updated on any client, then this callback is being received
	public void OnUserVariablesUpdate(BaseEvent evt)
	{		
	    ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
	    SFSUser user = (SFSUser)evt.Params["user"];
		
		if (user == smartFox.MySelf) return;
		
		if (!remoteWizards.ContainsKey(user)) 
		{
			//Debug.Log("new client update");
			// New client just started transmitting - lets create remote player
			Vector3 pos = new Vector3(0, 1, 0);
			if (user.ContainsVariable("x") && user.ContainsVariable("y") && user.ContainsVariable("z")) 
			{
				pos.x = (float)user.GetVariable("x").GetDoubleValue();
				pos.y = (float)user.GetVariable("y").GetDoubleValue();
				pos.z = (float)user.GetVariable("z").GetDoubleValue();
			}
			float rotAngle = 0;
			if (user.ContainsVariable("rot"))
			{
				rotAngle = (float)user.GetVariable("rot").GetDoubleValue();
			}
			
			/*
			int numMaterial = 0;
			if (user.ContainsVariable("mat")) {
				numMaterial = user.GetVariable("mat").GetIntValue();
			}*/ //color maybe?
			
			MakeWizard(user);
		}
		
	    // Check if the remote user changed his position or rotation
    	if (changedVars.Contains("x") && changedVars.Contains("y") && changedVars.Contains("z") && changedVars.Contains("rot"))
		{
			//Debug.Log("existing client update");
			remoteWizards[user].GetComponent<SimpleRemoteInterpolation>().SetTransform(
					new Vector3((float)user.GetVariable("x").GetDoubleValue(), (float)user.GetVariable("y").GetDoubleValue(), (float)user.GetVariable("z").GetDoubleValue()),
					Quaternion.Euler(0, (float)user.GetVariable("rot").GetDoubleValue(), 0),
					true);
    	}
	}
	
	private void OnObjectMessageReceived (BaseEvent evt){
		ISFSObject obj = (SFSObject)evt.Params["message"];
		
		//Fireball
		if(obj.GetUtfString("type").Equals("fireball"))
		{							
			Vector3 fbSpawnPoint = new Vector3(obj.GetFloat("positionX"), obj.GetFloat("positionY"), obj.GetFloat("positionZ"));
			Vector3 fbForward = new Vector3(obj.GetFloat("forwardX"), obj.GetFloat("forwardY"), obj.GetFloat("forwardZ"));		
				
			//Spawn fireball of other players	
			Fireball fb = ((GameObject)Instantiate(fireballPrefab, fbSpawnPoint, Quaternion.identity)).GetComponent<Fireball>();
			//fb.Owner = 	fbData.getUtfString("owner");
			User fbOwner = (User)evt.Params["sender"];
			if (remoteWizards.ContainsKey(fbOwner))
			{			
				fb.Owner = remoteWizards[fbOwner];
				fb.transform.forward = fbForward;
			}
		}
		//Health & Instability
		else if(obj.GetUtfString("type").Equals("stats"))
		{
			User characterHit = (User)evt.Params["sender"];
			if (remoteWizards.ContainsKey(characterHit))
			{			
				remoteWizards[characterHit].GetComponent<Wizard>().Health = obj.GetFloat("health");
				remoteWizards[characterHit].GetComponent<Wizard>().Instability = obj.GetFloat("instability");
				
			}
			
		}
		else if(obj.GetUtfString("type").Equals("transform") || obj.GetUtfString("type").Equals("teleport"))
		{
		    SFSUser user = (SFSUser)evt.Params["sender"];
			
			if (user == smartFox.MySelf) return;
			
			if (!remoteWizards.ContainsKey(user)) 
			{
				
				// New client just started transmitting - lets create remote player
				//Vector3 pos = new Vector3(obj.GetFloat("positionX"), obj.GetFloat("positionY"), obj.GetFloat("positionZ"));
				//float rotAngle = obj.GetFloat("rot");
				MakeWizard(user);
			}
			Boolean interp;
			if(obj.GetUtfString("type").Equals("transform"))
				interp = true;
			else
			{
				Instantiate(teleportOutEffect, remoteWizards[user].transform.position, remoteWizards[user].transform.rotation) ;
				Instantiate(teleportInEffect, new Vector3(obj.GetFloat("positionX"), obj.GetFloat("positionY"), obj.GetFloat("positionZ")), remoteWizards[user].transform.rotation);
				interp = false;
			}
			remoteWizards[user].GetComponent<SimpleRemoteInterpolation>().SetTransform(
					new Vector3(obj.GetFloat("positionX"), obj.GetFloat("positionY"), obj.GetFloat("positionZ")),
					Quaternion.Euler(0, obj.GetFloat("rot"), 0),
					interp);
		}
	}
	
	//draw gui for the game room
	void OnGUI (){
		GUI.skin = hudSkin;
		List<User> userList = currentRoom.UserList;
		GUI.BeginGroup(new Rect(20,10,140,360));
			GUI.Label(new Rect(0,0,50,25), "Players:", "randomText");
			//int k = 4; /*TODO - ADD CHECKS FOR DEATH*/
			for(int i=0; i<userList.Count; i++)
			{
		   		GUI.Label(new Rect(3,18+22*i,104,32),userList[i].Name, "randomText");
				/*User tempUser = userList[i];
				if(remoteWizards[tempUser].GetComponent<Wizard>().IsDead)
					GUI.Label(new Rect(0,20+32*i,icons[5].width,icons[5].height),icons[5]);
				else */
				GUI.Label(new Rect(0,20+icons[4].height*.75f*i,icons[4].width,icons[4].height*.75f),icons[4]);
			}		
		GUI.EndGroup();	
		
		GUILayout.BeginArea (new Rect (Screen.width - 160, 20, 140, 100));
			GUILayout.Label(currentRoom.Name);
			if(GUILayout.Button("Leave Room")){
				//clean up	
				remoteWizards = new Dictionary<User, GameObject>();
				smartFox.Send(new JoinRoomRequest("The Lobby"));
			}
		GUILayout.EndArea();
		
		/*TODO - SCOREBOARD*/
		if(Input.GetKey(KeyCode.Tab))
		{
			GUI.BeginGroup(new Rect(-5,Screen.height*0.1f,505,40+(50*userList.Count)),"Scoreboard","Box");
			 for(int i =0; i < userList.Count; i++)
			 {
				GUI.Label(new Rect(5,40+(50*i),500,50),"DoA" + " | " + userList[i].Name + " | " + "HP" + " | " + "INSTAB" + " | " + "WINS" + " | " + "KILLS" + " | " + "DEATHS" + " | " + "DMG DEALT");
			 }
			GUI.EndGroup();
		}
		
		//HUD
		GUI.BeginGroup(new Rect(0,Screen.height-(healthBar.height+icons[0].height+25), Screen.width, 140));
				GUI.BeginGroup(new Rect((Screen.width/2)-(healthBar.width/2)+10, 0, (localWizard.GetComponent<Wizard>().Health/100.0f)*(healthBar.width), healthBar.height));
					GUI.DrawTexture(new Rect(0,20,healthBar.width, healthBar.height/2), healthBar, ScaleMode.StretchToFill);
				GUI.EndGroup();
			string temp = localWizard.GetComponent<Wizard>().Health.ToString()+"/"+"100";
			GUI.Label(new Rect((Screen.width/2)-(healthBar.width/2) + 30, 10, healthBar.width, healthBar.height), localWizard.GetComponent<Wizard>().Health.ToString()+"/"+"100", "labelHealth");
		 
			//Instability
			GUI.Label(new Rect(Screen.width/2+healthBar.width*.55f, 0, 50, 50), localWizard.GetComponent<Wizard>().Instability.ToString()+"%", "labelInstab");
			GUI.Label(new Rect(Screen.width/2+healthBar.width*.58f, 28, 50, 50), "INSTABILITY", "labelInstabText");
			
			//Ability bar stuff-------------------------------------------------------------------------------------------------------------
			//even spaced positions for each ability
			List<Rect> butPos = new List<Rect>();
			for(int i = 0; i < 2; i++)
			{
				butPos.Add(new Rect(150 + healthBar.width/16.0f + (Screen.width/2)-(healthBar.width/2)+(healthBar.width/4*i), healthBar.height+18, icons[i].width, icons[i].height));
			}
			
			//
			if(!localWizard.GetComponent<Wizard>().fireballOnCD)
			{
				GUI.Button(butPos[0], new GUIContent("",icons[0],"Fireball does damage and knocks enemies back"));
			    GUI.Label(butPos[0],"M1","abiText");
			}
			else 
			{
				GUI.Label(butPos[0],"M1","abiText");
				string time = ((int)(localWizard.GetComponent<Wizard>().fireballGUICD) + 1).ToString();
				GUI.Button(butPos[0],icons[6]);// magic numbers for array... Ryan no like.
				GUI.Label(butPos[0],time,"timerText");
			}
			if(!localWizard.GetComponent<Wizard>().teleportOnCD)
			{
				GUI.Label(butPos[1],"Space","abiText");
				GUI.Button(butPos[1], new GUIContent("",icons[1],"Fireball does damage and knocks enemies back"));	
			}
			else 
			{
				GUI.Label(butPos[1],"Space","abiText");
				string time = ((int)(localWizard.GetComponent<Wizard>().teleportGUICD) + 1).ToString();
				GUI.Button(butPos[1],icons[7]);// magic numbers for array... Ryan no like.
				GUI.Label(butPos[1],time,"timerText"); 
			}
			/*if(!localWizard.GetComponent<Wizard>().reflectOnCD)
				GUI.Button(butPos[2], new GUIContent("",icons[0],"Fireball does damage and knocks enemies back"));	
			else 
			{
				string time = ((int)(localWizard.GetComponent<Wizard>().reflectGUICD)).ToString();
				GUI.Button(butPos[2],icons[0]);
				GUI.Label(butPos[2],time,"timerText");
			}*/
			//----------------------------------------------------------------------------------------------------------------
		
		GUI.EndGroup();
	}	
}






