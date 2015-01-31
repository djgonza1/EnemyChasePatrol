//Created by: David Gonzalez

using UnityEngine;
using System.Collections;

//I was going to call this InputManager but decided it would be conbused with Unity's InputManger
public class InputHandler : MonoBehaviour 
{
	PlayerController controller;
	public WaypointGraph waypointGraph;
	
	public Transform waypointA;
	public Transform waypointB;

	void Start ()
	{
		if(controller == null)
		{
			controller = GameObject.Find("Player").GetComponent<PlayerController>();
		}
		
	}
	
	//Update any objects with input here
	void Update () 
	{
		controller.UpdateController();
		
		if(Input.GetKeyDown(KeyCode.L))
		{
			waypointGraph.ShortestPath(waypointA, waypointB);
		}
	}
}
