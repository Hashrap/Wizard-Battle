using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	private string playerName;
	public string PlayerName { get { return playerName; } set { playerName = value; } }
	
	//Player holds TOTAL wins, losses, kills, damage, etc. and NOT current for the game. scoreboard will use these maybe?
	private int wins, losses, ties;
	public int Wins { get { return wins; } set { wins = value; } }
	public int Losses { get { return losses; } set { losses = value; } }
	public int Ties { get { return ties; } set { ties = value; } }
	
	private int kills, deaths;
	public int Kills { get { return kills; } set { kills = value; } }
	public int Deaths { get { return deaths; } set { deaths = value; } }
	
	private float damageDone, damageTaken;
	public float DamageDone { get { return damageDone; } set { damageDone = value; } }
	public float DamageTaken { get { return damageTaken; } set { damageTaken = value; } }
	
	//Reference to wizard the player is currently played // PS RENAME CHARACTER TO WIZARD
	
	/*
	private Wizard currentWizard;
	public Wizard CurrentWizard { get { return currentWizard; } set { currentWizard = value; } }
	*/
	
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
