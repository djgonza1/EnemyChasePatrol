﻿//Created by: David Gonzalez

using UnityEngine;
using System.Collections;

public class CameraBehaviour : MonoBehaviour {

	private GameObject player;

	void Start () 
	{
		player = GameObject.Find ("Player");
	}
	
	// Update is called once per frame
	void LateUpdate () 
	{
		Vector3 currentPos = player.transform.position;
		this.transform.position = new Vector3 (currentPos.x, currentPos.y, this.transform.position.z);
	}
}
