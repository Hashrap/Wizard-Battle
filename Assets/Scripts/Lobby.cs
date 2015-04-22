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


public class Lobby : MonoBehaviour {

	private SmartFox smartFox;
	private string zone = "m113bjl";
	private string serverName = "129.21.29.6";
	private int serverPort = 9933;
	public string username = "";
	private string loginErrorMessage = "";
	private bool isLoggedIn;
	
	private string newMessage = "";
	private ArrayList messages = new ArrayList();
		
	public GUISkin gSkin;
	
	//keep track of room we're in
	private Room currentActiveRoom;
	public Room CurrentActiveRoom{ get {return currentActiveRoom;} }
				
	private Vector2 roomScrollPosition, userScrollPosition, chatScrollPosition;
	private int roomSelection = -1;	  //For clicking on list box 
	private string[] roomNameStrings; //Names of rooms
	private string[] roomFullStrings; //Names and descriptions
	private int screenW;

	
	void Start()
	{
		Security.PrefetchSocketPolicy(serverName, serverPort); 
		bool debug = true;
		if (SmartFoxConnection.IsInitialized)
		{
			//If we've been here before, the connection has already been initialized. 
			//and we don't want to re-create this scene, therefore destroy the new one
			smartFox = SmartFoxConnection.Connection;
			Destroy(gameObject); 
		}
		else
		{
			//If this is the first time we've been here, keep the Lobby around
			//even when we load another scene, this will remain with all its data
			smartFox = new SmartFox(debug);
			DontDestroyOnLoad(gameObject);
		}
		
		smartFox.AddLogListener(LogLevel.INFO, OnDebugMessage);
		screenW = Screen.width;
	}
	
	private void AddEventListeners() {
		
		smartFox.RemoveAllEventListeners();
		
		smartFox.AddEventListener(SFSEvent.CONNECTION, OnConnection);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		smartFox.AddEventListener(SFSEvent.LOGIN, OnLogin);
		smartFox.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
		smartFox.AddEventListener(SFSEvent.LOGOUT, OnLogout);
		smartFox.AddEventListener(SFSEvent.ROOM_JOIN, OnJoinRoom);
		smartFox.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
		smartFox.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
		smartFox.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
		
		smartFox.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
		smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
		smartFox.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChange);
	}
	
	void FixedUpdate() {
		//this is necessary to have any smartfox action!
		smartFox.ProcessEvents();
	}
	
	private void UnregisterSFSSceneCallbacks() {
		smartFox.RemoveAllEventListeners();
	}
	
	public void OnConnection(BaseEvent evt) {
		bool success = (bool)evt.Params["success"];
		string error = (string)evt.Params["errorMessage"];
		
		Debug.Log("On Connection callback got: " + success + " (error? : <" + error + ">)");

		if (success) {
			SmartFoxConnection.Connection = smartFox;

			Debug.Log("Sending login request");
			smartFox.Send(new LoginRequest(username, "", zone));

		}
	}

	public void OnConnectionLost(BaseEvent evt) {
		Debug.Log("OnConnectionLost");
		isLoggedIn = false;
		UnregisterSFSSceneCallbacks();
		currentActiveRoom = null;
		roomSelection = -1;	
		Application.LoadLevel("The Lobby");
	}

	// Various SFS callbacks
	public void OnLogin(BaseEvent evt) {
		try {
			if (evt.Params.ContainsKey("success") && !(bool)evt.Params["success"]) {
				loginErrorMessage = (string)evt.Params["errorMessage"];
				Debug.Log("Login error: "+loginErrorMessage);
			}
			else {
				Debug.Log("Logged in successfully");
				PrepareLobby();	
			}
		}
		catch (Exception ex) {
			Debug.Log("Exception handling login request: "+ex.Message+" "+ex.StackTrace);
		}
	}

	public void OnLoginError(BaseEvent evt) {
		Debug.Log("Login error: "+(string)evt.Params["errorMessage"]);
	}
	
	void OnLogout(BaseEvent evt) {
		Debug.Log("OnLogout");
		isLoggedIn = false;
		currentActiveRoom = null;
		smartFox.Disconnect();
	}
	
	public void OnDebugMessage(BaseEvent evt) {
		string message = (string)evt.Params["message"];
		Debug.Log("[SFS DEBUG] " + message);
	}
	
	
	public void OnRoomAdded(BaseEvent evt)
	{
		Room room = (Room)evt.Params["room"];
		SetupRoomList();
		Debug.Log("Room added: "+room.Name);
	}
	public void OnRoomCreationError(BaseEvent evt)
	{
		Debug.Log("Error creating room");
	}
	
	public void OnJoinRoom(BaseEvent evt)
	{
		Room room = (Room)evt.Params["room"];
		currentActiveRoom = room;
		Debug.Log("joined "+room.Name);
		if(room.Name=="The Lobby" )
			Application.LoadLevel(room.Name);
		else {
			Application.LoadLevel("PreGameLobby");
			smartFox.Send(new SpectatorToPlayerRequest());
		}
	}
	
	public void OnUserEnterRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
			messages.Add( user.Name + " has entered the room.");
	}

	private void OnUserLeaveRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
		if(user.Name!=username){
			messages.Add( user.Name + " has left the room.");
		}	
	}

	public void OnUserCountChange(BaseEvent evt) {
		Room room = (Room)evt.Params["room"];
		if (room.IsGame ) {
			SetupRoomList();
		}
	}
	
	void OnPublicMessage(BaseEvent evt) {
		try {
			string message = (string)evt.Params["message"];
			User sender = (User)evt.Params["sender"];
			messages.Add(sender.Name +": "+ message);
			
			chatScrollPosition.y = Mathf.Infinity;
			Debug.Log("User " + sender.Name + " said: " + message); 
		}
		catch (Exception ex) {
			Debug.Log("Exception handling public message: "+ex.Message+ex.StackTrace);
		}
	}
	
	
	//PrepareLobby is called from OnLogin, the callback for login
	//so we can be assured that login was successful
	private void PrepareLobby() {
		Debug.Log("Setting up the lobby");
		SetupRoomList();
		isLoggedIn = true;
	}
	
	
	void OnGUI() {
		if (smartFox == null) return;
		screenW = Screen.width;
		GUI.skin = gSkin;
				
		// Login
		if (!isLoggedIn) {
			DrawLoginGUI();
		}
		
		else if (currentActiveRoom != null) 
		{
			
			// ****** Show full interface only in the Lobby ******* //
			if(currentActiveRoom.Name == "The Lobby")
			{
				DrawLobbyGUI();
				DrawRoomsGUI();
			}
		}
	}
	
	//Login GUI
	private void DrawLoginGUI(){
		GUI.BeginGroup(new Rect(Screen.width/2 - 150, Screen.height/2 - 150, 300, 300) );
			GUI.Label(new Rect(0, 0, 75, 50), "Username: ");
			username = GUI.TextField(new Rect(100, 2.5f, 150, 20), username, 25);
		
			if (GUI.Button(new Rect(2.5f, 125, 145, 24), "Login")  || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
			{
				AddEventListeners();
				smartFox.Connect(serverName, serverPort);
			}	
			else if(GUI.Button(new Rect(152.5f, 125, 145, 24), "Exit"))
			{
				//exit game
			}
		GUI.EndGroup();		

		GUI.Label(new Rect(10, 240, 100, 100), loginErrorMessage);

		
	}
			
	private void DrawLobbyGUI(){
		//GUI.Label(new Rect(2, -2, 680, 70), "", "SFSLogo");
		DrawUsersGUI();	
		DrawChatGUI();
		
		// Send message
		newMessage = GUI.TextField(new Rect(Screen.width * .05f, Screen.height * 6.4f/7.0f, Screen.width * .595f, Screen.height * .04f), newMessage, 50);
		if (GUI.Button(new Rect(Screen.width * .6475f, Screen.height * 6.375f/7.0f, 90, Screen.height * .045f), "Send")  || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
		{
			smartFox.Send( new PublicMessageRequest(newMessage) );
			newMessage = "";
		}
		/* Logout button
		if (GUI.Button (new Rect (screenW - 115, 20, 85, 24), "Logout")) {
			smartFox.Send( new LogoutRequest() );
		}*/
	}
		
		
	private void DrawUsersGUI(){
		GUI.Box (new Rect (Screen.width * .73f, Screen.height * 4.0f/7.0f, Screen.width * .22f, Screen.height * 1.0f/3.0f), "Users");
		GUILayout.BeginArea (new Rect (Screen.width * .73f, Screen.height * 4.2f/7.0f, Screen.width * .22f, Screen.height * 1.0f/3.0f));
			userScrollPosition = GUILayout.BeginScrollView (userScrollPosition, GUILayout.Width (150), GUILayout.Height (150));
			GUILayout.BeginVertical ();
			
				List<User> userList = currentActiveRoom.UserList;
				foreach (User user in userList) {
					GUILayout.Label (user.Name); 
				}
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
		GUILayout.EndArea ();
	}
	
	private void DrawRoomsGUI(){
		roomSelection = -1;
		GUI.Box (new Rect (Screen.width * .73f, Screen.height * 1.0f/7.0f, Screen.width * .22f, Screen.height * 1.0f/3.0f), "Room List");
		GUILayout.BeginArea (new Rect (Screen.width * .735f, Screen.height * 1.3f/7.0f, Screen.width * .22f, Screen.height * 1.0f/3.0f));
			if (smartFox.RoomList.Count >= 1) {		
				roomScrollPosition = GUILayout.BeginScrollView (roomScrollPosition, GUILayout.Width (180), GUILayout.Height (130));
					roomSelection = GUILayout.SelectionGrid (roomSelection, roomFullStrings, 1, "RoomListButton");
					
					if (roomSelection >= 0 && roomNameStrings[roomSelection] != currentActiveRoom.Name) {
						smartFox.Send(new JoinRoomRequest(roomNameStrings[roomSelection]));
					}
				GUILayout.EndScrollView ();
				
			} else {
				GUILayout.Label ("No rooms available to join");
			}
			
			// Game Room button
			if (currentActiveRoom.Name == "The Lobby"){
				if (GUI.Button (new Rect (80, 110, 85, 24), "Make Game")) {		
					//let smartfox take care of error if duplicate name
					RoomSettings settings = new RoomSettings(username + "'s Room");
					// how many players allowed
					settings.MaxUsers = 8;	
					settings.IsGame = true;
					
					//store indices into color arrays for setting user colors, delete as used
					SFSArray nums = new SFSArray();
					for(int i=0; i<8;i++){
						nums.AddInt(i);
					}
					//DEFAULTS AWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW MONEY
					SFSRoomVariable colorNums = new SFSRoomVariable("colorNums", nums);
					SFSRoomVariable host = new SFSRoomVariable("host", smartFox.MySelf.Id);
					SFSRoomVariable map = new SFSRoomVariable("map", (int)0);
					SFSRoomVariable mapSize = new SFSRoomVariable("mapSize", (int)2);
					SFSRoomVariable roundLength = new SFSRoomVariable("roundLength", (int)3);
					settings.Variables.Add(colorNums);
					settings.Variables.Add(host);
					settings.Variables.Add(map);
					settings.Variables.Add(mapSize);
					settings.Variables.Add(roundLength);
					settings.Extension = new RoomExtension(GameManager.ExtName, GameManager.ExtClass);
					smartFox.Send(new CreateRoomRequest(settings));
				
				}
			}
		GUILayout.EndArea();
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
		GUILayout.EndArea();		
	}
	
	
	
	
	private void SetupRoomList () {
		List<string> rooms = new List<string> ();
		List<string> roomsFull = new List<string> ();
		
		List<Room> allRooms = smartFox.RoomManager.GetRoomList();
		
		foreach (Room room in allRooms) {
			rooms.Add(room.Name);
			roomsFull.Add(room.Name + " (" + room.UserCount + "/" + room.MaxUsers + ")");
		}
		
		roomNameStrings = rooms.ToArray();
		roomFullStrings = roomsFull.ToArray();
		
		if (smartFox.LastJoinedRoom==null) {
			smartFox.Send(new JoinRoomRequest("The Lobby"));
		}
	}
}
