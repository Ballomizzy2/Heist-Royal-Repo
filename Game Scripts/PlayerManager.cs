using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Photon.Pun;


public class PlayerManager : MonoBehaviourPunCallbacks
{    
    [SerializeField]
    private CinemachineVirtualCamera vCam;
    private StarterAssets.ThirdPersonController controller;

    NetworkVariables variables;

    Vector3 setPosition;
    bool hasSetPosition;

    MenuManager menuManager;
    PhotonView view;

    GameManager gameManager;


    private void Start()
    {
        view = GetComponent<PhotonView>();

        if (view && !view.IsMine)
            return;

        gameManager = FindObjectOfType<GameManager>();
        variables = FindObjectOfType<NetworkVariables>();
        vCam = FindAnyObjectByType<CinemachineVirtualCamera>();
        controller = GetComponent<StarterAssets.ThirdPersonController>();
        SetUp_vCam();

        if(CheckIfIsCorrectCharcater() == false)
            ResetCharacter();

        // Check if in still menu, if so delete the player controller
        menuManager = FindObjectOfType<MenuManager>();

        if(menuManager != null)
        {
            Destroy(GetComponent<StarterAssets.ThirdPersonController>());
        }
        //controller.SetPlayerMultiplayerName(variables.GetVar(networkManager.LocalClientId));


    }

    private void Update()
    {
        if (view && !view.IsMine)
            return;

        if (!vCam)
        {
            vCam = FindAnyObjectByType<CinemachineVirtualCamera>();
        }
        if (hasSetPosition)
        {
            transform.SetPositionAndRotation(setPosition, Quaternion.identity);
            hasSetPosition = false;
        }
    }

    private void SetUp_vCam()
    {
        if (!view.IsMine)
            return;
        if(vCam != null)
        {
            vCam.Follow = transform.Find("PlayerCameraRoot");
            AdjustCameraPosition();
            /*DontDestroyOnLoad (vCam);
            DontDestroyOnLoad(Camera.main);*/
        }
    }

    public void AdjustCameraPosition()
    {
        vCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>().ShoulderOffset = new Vector3(0.4f, 0, -3F);
    }

    public void SetPlayerPosition(Vector3 pos)
    {
        setPosition = pos;
        hasSetPosition = true; 
        
    }

    public void ResetCharacter()
    {
        gameManager?.SpawnPlayer(this.gameObject);
    }

    private bool CheckIfIsCorrectCharcater()
    {
        return (this.gameObject.name == GetChosenCharacter());
    }

    private string GetChosenCharacter()
    {
        string name = PlayerPrefs.GetString("Chosen Character");


        /*if (name.Contains("(Clone)"))
        {
            name = name.Split('(')[0];
        }*/
        return name;
    }

}
