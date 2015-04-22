using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Logging;

public class PreGameLobby : MonoBehaviour {
	
	private bool gameOn;
	private bool ready = false;
	
	private SmartFox smartFox;
	private Room currentRoom;
	private List<User> userList;
	private string clientName;
	public string ClientName {
		get { return clientName; } }
	
	//GUI stuff
	public GUISkin lobbySkin;
	public Texture2D[] readyTex;
	public Texture2D[] bar1;
	public Texture2D[] bar2;
	public Texture2D[] bar3;
	public Texture2D[] bar4;
	private bool dirtySettings = true;
	private Vector2 chatScrollPosition;
	private string newMessage = "";
	private ArrayList messages = new ArrayList();
	
	//Settings variables
	private int abl1=0, abl2=0, abl3=0, abl4=0, roundLength = 3, mapSize = 2, map=0;
	private string[] mapSizes = {"Tiny","Small","Medium","Large","Huge"};
	private string[] maps = {"Default","Baako's Grave","Well of Sergjan","Kojo Pygmy Village"};
	
	// Use this for initialization
	void Start () {
		bool debug = true;
		gameOn = true;
		if (SmartFoxConnection.IsInitialized)
			smartFox = SmartFoxConnection.Connection;
		else
			smartFox = new SmartFox(debug);
		
		currentRoom = smartFox.LastJoinedRoom;
		clientName = smartFox.MySelf.Name;
		
		StartCoroutine(refreshTick(0f));
		SubscribeDelegates();
	}
	
	// Update is called once per frame
	void Update () {
		if(dirtySettings) {
			StartCoroutine(refreshTick(0.5f));
			dirtySettings = false;
		}
	}
	
	void FixedUpdate() {
		if (!gameOn) return;
		smartFox.ProcessEvents();
	}
	
	void OnGUI() {
		//player list/info
 		userList = currentRoom.UserList;
		int k=0, i=0;
		GUI.BeginGroup(new Rect(Screen.width/2-200,10,400,200));
		 foreach (User user in userList)
		 {
			k=0;
			
			//GUI.Label(new Rect(20,25*i,375,25),user.Name+" ("+user.GetVariable("primary").GetIntValue()+","+user.GetVariable("secondary").GetIntValue()+","+user.GetVariable("defense").GetIntValue()+","+user.GetVariable("movement").GetIntValue()+")","Box");
			
			GUI.Label(new Rect(20,25*i,375,25),user.Name /*+" ("
				+user.GetVariable("primary").GetIntValue()+","
				+user.GetVariable("secondary").GetIntValue()+","
				+user.GetVariable("defense").GetIntValue()+","
				+user.GetVariable("defense").GetIntValue()+")"*/,"Box");
			
			
			if(user.ContainsVariable("ready") && user.GetVariable("ready").GetBoolValue() == true)
				k = 1;
			GUI.Label(new Rect(0,5+(25*i),readyTex[k].width,readyTex[k].height), readyTex[k]);
			i++;
		 }
		GUI.EndGroup();
		
		//ability customization
		/*
		if(!ready)
		{
			GUI.BeginGroup(new Rect(Screen.width-256, 10, 192, 256));
			 abl1 = GUI.Toolbar(new Rect(0,0,64*3,64),abl1,bar1);
			 abl2 = GUI.Toolbar(new Rect(0,64,64*3,64),abl2,bar2);
			 abl3 = GUI.Toolbar(new Rect(0,128,64*3,64),abl3,bar3);
			 abl4 = GUI.Toolbar(new Rect(0,192,64*3,64),abl4,bar4);
			GUI.EndGroup();
		}
		*/
		
		//Ready and Leave buttons
		if(GUI.Button(new Rect(Screen.width-256,266,192,50),"Ready")) {
			if(ready)
				ready=false;
			else
				ready=true;
		}
		if(GUI.Button(new Rect(Screen.width-206,316,100,25),"Leave Game")) {
			//leave game
			smartFox.Send(new JoinRoomRequest("The Lobby"));
		}
		
		//game info
		GUI.BeginGroup(new Rect(Screen.width*.1f,10,200,75),"Game Settings","Box");
		 GUI.Label(new Rect(10,20,300,25),"Round length: "+roundLength+" minutes");
		 //GUI.Label(new Rect(10,45,300,25),"Map: "+maps[map]);
		 GUI.Label(new Rect(10,45,300,25),"Map Size: "+mapSizes[mapSize]);
		GUI.EndGroup();
		
		//Host setting controls
		if(currentRoom.GetVariable("host").GetIntValue() == smartFox.MySelf.Id)
			DrawHostGUI();
		
		//send chat message
		newMessage = GUI.TextField(new Rect(Screen.width * .05f, Screen.height * 6.4f/7.0f, Screen.width * .595f, 30), newMessage, 50);
		if (GUI.Button(new Rect(Screen.width * .6475f, Screen.height * 6.4f/7.0f, 90, 30), "Send")  || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
		{
			smartFox.Send(new PublicMessageRequest(newMessage));
			newMessage = "";
		}
		DrawChatGUI();
	}
	
	private void DrawHostGUI(){
		bool gameReady = true;
		foreach(User user in currentRoom.UserList)
		{
			if(!user.GetVariable("ready").GetBoolValue())
				gameReady = false;
		}
		if(GUI.Button(new Rect(Screen.width/2, Screen.height/2, 100, 25),"Start") && gameReady == true) {
			//Set
			smartFox.Send(new ObjectMessageRequest(new SFSObject()));
			//unload delegates
			UnsubscribeDelegates();
			//Switch scenes to the arena
			Application.LoadLevel("arena01");
		}
		GUI.BeginGroup(new Rect(Screen.width*.05f,10,60,75),"Controls","Box");
		 if(GUI.Button(new Rect(5,20,25,25),"<")&& roundLength > 1)
			roundLength--;
		 if(GUI.Button(new Rect(30,20,25,25),">")&& roundLength < 15)
			roundLength++;
		 /*if(GUI.Button(new Rect(5,45,25,25),"<")&& map > 0)
			map--;
		 if(GUI.Button(new Rect(30,45,25,25),">")&& map < maps.Length-1)
			map++;*/
		 if(GUI.Button(new Rect(5,45,25,25),"<")&& mapSize > 0)
			mapSize--;
		 if(GUI.Button(new Rect(30,45,25,25),">")&& mapSize < mapSizes.Length-1)
			mapSize++;
		GUI.EndGroup();
	}
	
	private void DrawChatGUI(){
		GUI.Box(new Rect(Screen.width * .05f, Screen.height * 4.0f/7.0f, Screen.width * 2.0f/3.0f, Screen.height * 1.0f/3.0f), "Chat");

		GUILayout.BeginArea (new Rect(Screen.width * .105f, Screen.height * 4.25f/7.0f, Screen.width * 2.0f/3.0f, Screen.height * 1.0f/3.0f));
			chatScrollPosition = GUILayout.BeginScrollView (chatScrollPosition, GUILayout.Width (450), GUILayout.Height (350));
				GUILayout.BeginVertical();
					foreach (string message in messages) {
						//this displays text from messages arraylist in the chat window
						GUILayout.Label(message);
					}
				GUILayout.EndVertical();
			GUILayout.EndScrollView ();
		GUILayout.EndArea();;		
	}
	
	IEnumerator refreshTick(float time)
	{
		
		SFSUserVariable[] vars = new SFSUserVariable[4];
		vars[0] = new SFSUserVariable("primary",abl1);
		vars[1] = new SFSUserVariable("secondary",abl2);
		vars[2] = new SFSUserVariable("defense",abl3);
		//vars[3] = new SFSUserVariable("movement",abl4);
		//vars[4] = new SFSUserVariable("ready", ready);
		
		vars[3] = new SFSUserVariable("ready", ready);
		
		smartFox.Send(new SetUserVariablesRequest(vars));
		if(currentRoom.GetVariable("host").GetIntValue() == smartFox.MySelf.Id)
		{
			SFSRoomVariable[] rVars = new SFSRoomVariable[3];
			rVars[0] = new SFSRoomVariable("map",map);
			rVars[1] = new SFSRoomVariable("mapSize",mapSize);
			rVars[2] = new SFSRoomVariable("roundLength",roundLength);
			
			smartFox.Send(new SetRoomVariablesRequest(rVars));
		}
		else
		{
			map = currentRoom.GetVariable("map").GetIntValue();
			mapSize = currentRoom.GetVariable("mapSize").GetIntValue();
			roundLength = currentRoom.GetVariable("roundLength").GetIntValue();
		}
		yield return new WaitForSeconds(time);
		dirtySettings = true;
	}
	
	void SubscribeDelegates(){
		// listen for an smartfox events 
		smartFox.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
		smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
		smartFox.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChange);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		smartFox.AddEventListener(SFSEvent.SPECTATOR_TO_PLAYER, OnSpectatorToPlayer);
		smartFox.AddEventListener(SFSEvent.SPECTATOR_TO_PLAYER_ERROR, OnSpectatorToPlayerError);
		smartFox.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessageReceived);
		smartFox.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVariablesUpdate); 
		smartFox.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
	}
	
	private void UnsubscribeDelegates() {
		smartFox.RemoveAllEventListeners();
	}
	
	public void OnUserEnterRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
		Debug.Log ("user entered room " + user.Name);
	}
	
	public void OnUserLeaveRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
		/* TODO - CAN PROBABLY DELETE THIS */
		//Destroy(others[user.Name]);
		//others.Remove(user.Name);
	}
	
	public void OnUserCountChange(BaseEvent evt) {
		Debug.Log("Game room user count changed");
	}
	
	public void OnConnectionLost(BaseEvent evt) {
		UnsubscribeDelegates();
		Screen.lockCursor = false;
		Screen.showCursor = true;
		Application.LoadLevel("The Lobby");
	}
	
	public void OnSpectatorToPlayer(BaseEvent evt) {
		User user = (User)evt.Params["user"];
	}
	
	public void OnSpectatorToPlayerError(BaseEvent evt) {
		User user = (User)evt.Params["user"];
	}
	
	public void OnObjectMessageReceived(BaseEvent evt) {
		if ((User)evt.Params["sender"] == smartFox.MySelf) return;
		Debug.Log("obj msg received!  Unloading SFS delegates...");
		UnsubscribeDelegates();
		Debug.Log("Loading game...");
		Application.LoadLevel("arena01");
	}
	
	void OnPublicMessage(BaseEvent evt) {
		try {
			string message = (string)evt.Params["message"];
			User sender = (User)evt.Params["sender"];
			messages.Add(sender.Name +": "+ message);
			
			chatScrollPosition.y = Mathf.Infinity;
		}
		catch (Exception ex) {
		}
	}
	
	public void OnUserVariablesUpdate(BaseEvent evt) {
		//List<UserVariable> changedVars = (List<UserVariable>)evt.Params["changedVars"];
        //User user = (User)evt.Params["user"];
	}
}
