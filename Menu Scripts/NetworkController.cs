using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkController : MonoBehaviourPunCallbacks
{
    public bool hasJoinedLobby { get; private set; }
    PhotonView view;
    MenuManager menuManager;


    // Start is called before the first frame update
    void Start()
    {
        menuManager = FindAnyObjectByType<MenuManager>();
        PhotonNetwork.ConnectUsingSettings(); //Connects to Photon Masters Servers

        GameObject respawner = GameObject.FindGameObjectWithTag("Respawn");
        if (respawner)
        {
            hasJoinedLobby = true;
            menuManager.OnJoinedRoom();
            Destroy(respawner);
        }
    }

    
    public override void OnConnectedToMaster()
    {
        string log = "Connected to the Ballomizzy " + PhotonNetwork.CloudRegion + " server!";
        menuManager.LogText(log, Color.green, 1f);
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        menuManager.LogText("Joined lobby", Color.green, 2f);
        hasJoinedLobby = true;
        //SceneManager.LoadScene("Game");
    }

    public void SpawnPlayer()
    {

    }
}
