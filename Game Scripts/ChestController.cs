using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ChestController : MonoBehaviour
{
    private StarterAssets.ThirdPersonController playerOpeningTheChest;
    private Animator animator;
    [SerializeField] private GameObject keyAnimation;
    //[SerializeField] private Outline outline;

    private void Start()
    {
        animator = GetComponent<Animator>();
        keyAnimation.SetActive(false);
    }
    public void OpenChest(StarterAssets.ThirdPersonController _player)
    {
        //Spawn Key
        playerOpeningTheChest = _player;
        SpawnKey();
        //Animation
        animator.SetTrigger("Open");
        //VFX and SFX
        //outline.enabled = false;
        //Destroy Chest

        Invoke("DestroyGameObject", 1.5f);
    }

    private void DestroyGameObject()
    {
        PhotonNetwork.Destroy(gameObject);
    }
    public void SpawnKey()
    {
        int rand = Random.Range(0,2);
        Debug.Log(rand);
        if(rand == 1)
        {
            //spawn the key
            //Let the player know that they have a key
            keyAnimation.SetActive(true);
            playerOpeningTheChest.GetKey();

        }
    }

}
