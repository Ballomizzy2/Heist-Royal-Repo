using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameBehaviour : MonoBehaviour
{
    
    PlayerManager playerManager;
    Camera mainCam;

    [SerializeField]
    private TextMeshPro playerName;

    PhotonView view;

    private void Start()
    {
        mainCam = Camera.main;
        view = GetComponent<PhotonView>();
        if (!view.IsMine)
            return;
        playerManager = GetComponent<PlayerManager>();

        playerName = GetComponent<TextMeshPro>();

        string name = GetPlayerName();
        SetName(name);
        view.RPC("SetPlayerName", RpcTarget.OthersBuffered, view.ViewID, name);
    }

    // RPCS
    [PunRPC]
    void SetPlayerName(int viewID, string name)
    {
        PhotonView.Find(viewID).GetComponent<PlayerNameBehaviour>().SetName(name);
    }

    private void SetName(string name)
    {
        if (playerName != null)
            playerName.text = name;
    }


    // Update is called once per frame
    private void Update()
    {
        /*if (!view.IsMine)
            return;*/
        if (mainCam != null)
        {
            transform.LookAt(mainCam.transform.position);
            transform.forward = - transform.forward;
            //transform.rotation = new Quaternion(0, transform.rotation.y, 0, 0);
        }
    }

    private string GetPlayerName()
    {
        return PlayerPrefs.GetString("Player Name");
    }



}
