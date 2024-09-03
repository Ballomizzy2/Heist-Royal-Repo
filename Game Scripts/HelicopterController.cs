using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
    private Transform playerTarget;
    private GameManager gameManager;

    [SerializeField]
    private Transform helicopterTorch, torchRaySource;

    [SerializeField]
    private Collider collisionSphere;

    [SerializeField]
    private bool hasFoundPlayer;


   

    float x, y, z;
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        //playerTarget = gameManager.playerWithBag.transform;

    }

    private void Update()
    {

        if(playerTarget != null)
        {
            x = playerTarget.position.x;
            y = playerTarget.position.y + 11;
            z = playerTarget.position.z - 8;
            transform.position = new Vector3(x, y, z);
            transform.LookAt(playerTarget.position);
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, transform.localEulerAngles.z);
            hasFoundPlayer = true;
        }
        else
        {
            hasFoundPlayer = false;

        }

    }

    public void FoundPlayer(StarterAssets.ThirdPersonController player)
    {
        hasFoundPlayer = true;
        playerTarget = player.transform;
        gameManager.AllEnemiesChasePlayer(player.transform);
    }

    public bool GetHasFoundPlayer() { return hasFoundPlayer; }

}
