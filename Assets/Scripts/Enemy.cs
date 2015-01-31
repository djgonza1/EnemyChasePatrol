using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EnemyState
{
	PATROL,
	CHASING,
	RETURNING,
    DEAD
}

enum EnemyVisionState
{
    NORMAL,
    BOOSTED
}

public class Enemy : Character 
{
	public Transform[] patrolPath;
	
	public EnemyState state;
	
	private Player player;
	private WaypointGraph waypointGraph;
	private PlayerTrail playerTrail;
    private EnemyVisionState vision = EnemyVisionState.NORMAL;
	private SpriteRenderer sprite;
	private SpriteRenderer minimapSprite;
	private  Transform LoSCollider;
	
	void Start () 
	{
		sprite = transform.FindChild ("EnemyPlaceholder").GetComponent<SpriteRenderer>();
		minimapSprite = transform.FindChild ("Minimap EnemyPlaceholder").GetComponent<SpriteRenderer>();
        LoSCollider = transform.FindChild("LineOfSight");
		player = GameObject.Find("Player").GetComponent<Player> ();
		playerTrail = player.GetComponent<PlayerTrail>();
		waypointGraph = GameObject.Find("WaypointGraph").GetComponent<WaypointGraph>();
		
		
		StartCoroutine("Patrol");
	}
	
	// Update is called once per frame
	void Update () 
	{
		//followPlayer();
		
	}
	
	IEnumerator FollowPlayer()
	{
		this.state = EnemyState.CHASING;
		
		string[] sightLayers = {"LightWalls", "Mobs"};
		LayerMask sightMask = LayerMask.GetMask(sightLayers);
		
		
		while(this.state == EnemyState.CHASING)
		{
			Vector2 rayDir = player.transform.position - this.transform.position;
			RaycastHit2D hit = Physics2D.Raycast(this.transform.position, rayDir, 1000, sightMask);
			
			if(hit && hit.transform == player.transform && player.state != PlayerState.STEALTH)
			{
				WalkTowards(player.transform.position);
			}
			else
			{
				TrailCrumb crumbToFollow = null;
				string[] trackLayers = {"LightWalls", "Tracks"};
				LayerMask trackMask = LayerMask.GetMask(trackLayers);
				
				foreach(TrailCrumb crumb in playerTrail.trail)
				{
					rayDir = crumb.transform.position - this.transform.position;
					RaycastHit2D[] hitArray = Physics2D.RaycastAll(this.transform.position, rayDir, 1000, trackMask);
					
					for(int i = 0; i < hitArray.Length && hitArray[i].transform.tag == "Trail"; i++)
					{
						TrailCrumb hitCrumb = hitArray[i].transform.GetComponent<TrailCrumb>();
						
						if(crumbToFollow == null || crumbToFollow.GetLifeTime() > hitCrumb.GetLifeTime())
						{
							crumbToFollow = hitCrumb;
						}
					}
				}
				
				if(crumbToFollow != null)
				{
					WalkTowards(crumbToFollow.transform.position);
				}
				else
				{
					OnPlayerLost();
				}
			}
			
			yield return new WaitForFixedUpdate();
		}
	}
	
	IEnumerator Patrol()
	{
		state = EnemyState.PATROL;
		
		int i = ClosestWaypoint ();
		while(state == EnemyState.PATROL)
		{
			
			Vector2 to = patrolPath[i].position;
			
			while( (Vector2)this.transform.position != to && state == EnemyState.PATROL )
			{
				WalkTowards(to);
				
				yield return null;
			}
			
			
			i = (i >= patrolPath.Length - 1)? 0 : ++i;
		}
	}
	
	IEnumerator ReturnToPatrol()
	{
		state = EnemyState.RETURNING;
		
		RaycastHit2D[] hitArray;
		string[] layers = {"Enemy", "LightWalls"};
		LayerMask mask = LayerMask.GetMask(layers);
		
		Transform closestWaypoint = null;
		for(int i = 0; i < waypointGraph.waypoints.childCount; i++)
		{
			Transform current = waypointGraph.waypoints.GetChild(i);
			Vector2 rayDir = this.transform.position - current.position;
			hitArray = Physics2D.RaycastAll(current.position, rayDir, 1000, mask);
			
			
			for(int j = 0; j < hitArray.Length && hitArray[j].transform.tag != "Wall"; j++)
			{
				if(hitArray[j].transform == this.transform)
				{
					if(closestWaypoint == null)
					{
						closestWaypoint = current;
					}
					else 
					{
						float currentDistance = (current.position-this.transform.position).magnitude;
						float closestDistance = (closestWaypoint.position-this.transform.position).magnitude;
						
						if(currentDistance < closestDistance)
						{
							closestWaypoint = current;
						}
					}
				}
			}
		}
		
		while((Vector2)this.transform.position != (Vector2)closestWaypoint.position && state == EnemyState.RETURNING)
		{
			WalkTowards(closestWaypoint.position);
			
			yield return null;
		}
		
		Transform targetNode = patrolPath[0];
		LinkedList<Transform> shortestPath = waypointGraph.ShortestPath(closestWaypoint, targetNode);
		
		while(shortestPath.Count > 0 && state == EnemyState.RETURNING)
		{
			Vector2 toPosition = shortestPath.First.Value.position;
			
			while((Vector2)this.transform.position != toPosition && state == EnemyState.RETURNING)
			{
				WalkTowards(toPosition);
				
				yield return null;
			}
			
			shortestPath.RemoveFirst();
			
			if(shortestPath.Count > 0)
			{
				toPosition = shortestPath.First.Value.position;
			}
			
		}
		
		if((Vector2)this.transform.position == (Vector2)targetNode.position)
		{
			StartCoroutine("Patrol");
		}
		
		
	}
	
	
	
	private void WalkTowards(Vector2 to)
	{
		//Vector2 direction = Vector3.Normalize(to - (Vector2)this.transform.position);
		Vector2 direction = to - (Vector2)this.transform.position;
		this.transform.Translate(Vector3.ClampMagnitude(direction, speed*Time.deltaTime), Space.World);
		
		if((Vector2)this.transform.position != to)
		{
			this.transform.right = to - (Vector2)this.transform.position;
		}
	}

	private int ClosestWaypoint()
	{
		int nearest = 0;
		for ( int i = 0; i<this.patrolPath.Length; i++)
		{
			float distance = (this.transform.position - this.patrolPath[i].transform.position).magnitude;
			if(distance <(this.transform.position - this.patrolPath[nearest].transform.position).magnitude)
			{
				nearest = i;
			}
		}
		
		return nearest;
	}
	
	public void OnPlayerSighted()
	{
		if(state != EnemyState.CHASING)
		{
			StartCoroutine("FollowPlayer");
		}
		
	}
	
	public void OnPlayerLost()
	{
		if(state == EnemyState.CHASING)
		{
			StartCoroutine("ReturnToPatrol");
		}
	}

    public void GetHit(float damage)
    {
        health = health - damage;
        if (health <= 0)
        {
            Die();
        }
    }

	public void Die()
	{

		this.sprite.enabled = false;
		this.minimapSprite.enabled = false;
		this.collider2D.enabled = false;
        this.LoSCollider.GetComponent<PolygonCollider2D>().enabled = false;
        state = EnemyState.DEAD;
	}

    public void BoostSight()
    {
        if (vision == EnemyVisionState.NORMAL)
        {
            vision = EnemyVisionState.BOOSTED;
            Debug.Log("BOOST!");
            LoSCollider.localScale = new Vector3(LoSCollider.localScale.x * 2, LoSCollider.localScale.y, LoSCollider.localScale.z);
        }
    }

    public void NormalSight()
    {
        if (vision == EnemyVisionState.BOOSTED)
        {
            vision = EnemyVisionState.NORMAL;
            Debug.Log("BOOST!");
            LoSCollider.localScale = new Vector3(LoSCollider.localScale.x / 2, LoSCollider.localScale.y, LoSCollider.localScale.z);
        }
    }
    
	void OnCollisionStay2D(Collision2D coll)
	{
		if(coll.transform.tag == "Wall")
		{
			Vector2 wallNormal = coll.contacts[0].normal;
			Vector2 wallParallel = new Vector2(wallNormal.y, -wallNormal.x);
			Vector2 aimDirection = this.transform.right * speed;
			Vector2 currentVelocity = Vector3.Project(aimDirection, wallParallel);
			
			float lostSpeed = speed - currentVelocity.magnitude;
			Vector2 lostVelocity = Vector3.Normalize(currentVelocity) * lostSpeed;
			this.transform.Translate(lostVelocity*Time.deltaTime, Space.World);
		}
	}
}

