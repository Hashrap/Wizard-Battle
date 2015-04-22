using UnityEngine;
using System.Collections;

public class SafeAreaCylinder : MonoBehaviour 
{
	private bool shrinkStage = false;
	private float stageScaleRatio; // ratio of the stage size to the match length
	private const float TINY_STAGE = 60.0f;
	private const float SMALL_STAGE = 80.0f;
	private const float MEDIUM_STAGE = 120.0f;
	private const float LARGE_STAGE = 180.0f;
	private const float HUGE_STAGE = 240.0f;

	private float pregameDelay = 10.0f;
	private float currentTime;
	private float matchTime; // should only ever get set once
	private bool matchTimeSet = false;
	
	//Gui info for timer
	private Vector2 timerTopLeft;
	private float timerWidth;
	private float timerHeight;
	private Rect timerRect;
	
	public float originalStageScale;
	private string timeString = "";
	private string minutesString = "";
	private string secondsString = "";
	private string millisecString = "";
	
	public enum StageSize {TINY, SMALL, MEDIUM, LARGE, HUGE}
	
	// Use this for initialization
	void Start () 
	{
		timerWidth = 200.0f;
		timerHeight = 100.0f;
		float tempx = Screen.width/2 - timerWidth/2;
		timerTopLeft = new Vector2(tempx, 20.0f);
		timerRect = new Rect(timerTopLeft.x, timerTopLeft.y, timerWidth, timerHeight);
	}
	
	/// <summary>
	/// Sets the stage scale and match length.
	/// </summary>
	/// <param name='stageSize'>
	/// The size of the stage to be created. (tiny, small, medium, large, huge)
	/// </param>
	/// <param name='timeInSeconds'>
	/// The match length in seconds. Needs to end in .0f
	/// </param>
	public void set_stage(int stageSize, float timeInMinutes)
	{
		StageSize size = (StageSize)stageSize;
		float radius = 0f;
		switch((int)size)
		{
			case 0:
				radius = TINY_STAGE;
				break;
			case 1:
				radius = SMALL_STAGE;
				break;
			case 2:
				radius = MEDIUM_STAGE;
				break;
			case 3:
				radius = LARGE_STAGE;
				break;
			case 4:
				radius = HUGE_STAGE;
				break;
			default:
				radius = MEDIUM_STAGE;
				break;
		}
		setStageScale(radius);
		setMatchLength(timeInMinutes*60.0f);
	}
	
	/// <summary>
	/// Sets the stage scale.
	/// </summary>
	/// <param name='radius'>
	/// The radius of the safe area.
	/// </param>
	private void setStageScale(float radius)
	{
		originalStageScale = radius;
		Vector3 stageStartScale = new Vector3(originalStageScale, this.transform.localScale.y, originalStageScale);
		this.transform.localScale = stageStartScale;
	}
	
	/// <summary>
	/// Sets the length of the match.  Can only be called once per game, call wisely.
	/// Also, make sure that the stage scale is set before calling this function or
	/// else the stage may not scale at all.
	/// </summary>
	/// <returns>
	/// A boolean telling whether or not the operation was allowed.  If false then the 
	/// match time has already been set.
	/// </returns>
	/// <param name='timeInSeconds'>
	/// If set to <c>true</c> time in seconds.
	/// </param>
	private bool setMatchLength(float timeInSeconds)
	{
		if(!matchTimeSet)
		{
			matchTime = timeInSeconds;
			matchTimeSet = true;
			setStageScaleRatio();
			currentTime = matchTime;
			return true;
		}
		return false;
	}
	
	// called after the match length has been set
	private void setStageScaleRatio()
	{
		stageScaleRatio = (originalStageScale/matchTime);
		/*Debug.Log("orig stage scale: " + originalStageScale);
		Debug.Log("match time: " + matchTime);
		Debug.Log("Stage Scale Ratio: " + stageScaleRatio);*/
	}
	
	//  *idea for networking shits later*
	//if stage scale is off, call setStageScale on all clients from the server with a 1 scale
	// value 1 seconds in the future.
	// on each client grab the new scale info, and the time delay from server to client, subtract
	// time delay from 1 second and after that time has elapsed apply the new scale info
	
	// Update is called once per frame
	void Update () 
	{
		
		if(pregameDelay < 0.0f && pregameDelay > -1.0f)
		{
			shrinkStage = true;
			pregameDelay = -2.0f;
		}
		else{
			pregameDelay -= Time.deltaTime;
		}
		
		if(shrinkStage)
		{
			
			// creates a vector 3 that contains the scaling for the new frame
			Vector3 scaleDifference = this.transform.localScale;
			scaleDifference.x -= Time.deltaTime * stageScaleRatio;
			scaleDifference.z = scaleDifference.x;
			
			// sets the gameobject's scale value to the new scale value
			this.transform.localScale = scaleDifference;
			
			// grabs the material for the object and scales it relative to the amount the stage shrank
			MeshRenderer mr = this.gameObject.GetComponent<MeshRenderer>();
			mr.material.mainTextureScale = new Vector2(scaleDifference.x/30, scaleDifference.z/30);
			
			// if the stage is about to disappear,
			// and subsequently invert itself,
			// stop it and move it out of the way
			if(this.transform.localScale.x < 0.1f)
			{
				shrinkStage = false;
				this.transform.Translate(0, -10, 0);
			}
			
			// adjust time
			currentTime -= Time.deltaTime;
			
			// If the shrinkStage flag just got set to false then the game is still
			// going to run the above line and subtract the deltaTime from the time
			// so we set the time to 0 afterwards to prevent negative numbers.
			// Contrary to popular belief, this is not an unreachable code block
			if(!shrinkStage)
			{
				// Extra zeros are for time formatting, even though they don't make a difference
				// its more of a "this is how the string should look" deal.
				currentTime = 000000.0f;
			}
		}
	}
	
	public void Reset()
	{
		this.transform.localScale = new Vector3(originalStageScale, this.transform.localScale.y, originalStageScale);
		currentTime = matchTime;
		pregameDelay = 10.0f;
		shrinkStage = false;
	}
	
	void OnGUI()
	{
		
		timeString = currentTime.ToString();
		// only trim output if it is too long
		if(timeString.Length > 6)
			timeString = timeString.Remove(6);
		if(pregameDelay > 0)
			timeString += ".0";
		int temp = timeString.IndexOf(".");
		
		// create the milliseconds string
		millisecString = timeString.Substring(temp + 1);
		if(millisecString.Length < 2){
			millisecString = "0" + millisecString;
		}
		
		// create the minutes string
		minutesString = (currentTime/60).ToString();
		minutesString += "00";
		minutesString = minutesString.Remove(1);
		
		// create the seconds string
		float mins = currentTime/60;
		int numMins = Mathf.FloorToInt(mins);
		temp = (int)(currentTime - (numMins * 60));
		secondsString = temp.ToString();
		
		
		if(secondsString.Length < 2){
			secondsString = "0" + secondsString;
		}
		
		string outputString = minutesString + ":" + secondsString + ":" + millisecString;
		
		// display time remaining
		GUI.BeginGroup(timerRect);
		GUI.Label(new Rect(0,0,200,50), "Time Until Stage Is Gone");
		GUI.Label(new Rect(50,20,100,50), outputString);
		GUI.EndGroup();
	}
}
