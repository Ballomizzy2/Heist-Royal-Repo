using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{

    [SerializeField]
    List<GameObject> respawnPoints = new List<GameObject>();

    [SerializeField]
    GameObject playerPrefab;

    [SerializeField]
    private CinemachineVirtualCameraBase vCam;

    PhotonView view;

    private Camera cam = null;

    public Transform getCam()
    {
        return cam.transform;
    }    

    [SerializeField]
    public Collider bankBounds;


    public enum GameStates
    {

        // Start game phase is where we have the players arrive to the island
        // Search game phase is where we have the players search for the money in the bank
        // Chase game phase is where one of the players robs the money from the bank and is being chased
        Start, Search, Chase, Finish
    }

    private GameStates gameState;

    public enum Characters { Apple, Orange, Grape, Banana, Pineapple }
    [SerializeField]
    private Characters chosenCharacter;
    [SerializeField] private GameObject theChaseAlarms;

    //Money Bag stuff
    [SerializeField]
    private GameObject moneyBagGO;
    [SerializeField]
    private float dropDistance = .5f;
    public GameObject playerWithBag;

    //Helicopter stuff
    [SerializeField]
    private GameObject helicopterGO;
    [SerializeField]
    private Transform heliSpawnPoint;
    private GameObject spawnedHelicopter;

    public bool allEnemiesChasePlayer;
    public Transform playerToChase;
    [SerializeField]
    public GameObject spawnedPlayer;

    //Multiplayer Stuff
    NetworkVariables networkVariables;
    private bool hasSpawnedPlayersAtSpawnPoints;

    private bool checkForOtherGameManager, otherGameManagerFound;

    [SerializeField]
    private string playerServerName;

    [SerializeField]
    private List<GameObject> playersInServer = new List<GameObject>();

    // Game Manager 
    private GameManager instance;

    // Game UI
    [SerializeField] private GameUIManager gameUI;


    // Game End return to Menu
    private bool hasSpawnedRespawner;



    void Start()
    {          
        view = GetComponent<PhotonView>();

        /*if (view && !view.IsMine)
        {
            //Destroy(gameObject);
            return;
        }*/


        //In Game stuff
        gameState = GameStates.Start;
        moneyBagGO.SetActive(false);
        theChaseAlarms.SetActive(false);

        //SpawnPlayer(); 
    }


    // Update is called once per frame
    void Update()
    {
        /*if (!view.IsMine)
            return;*/

        if (!moneyBagGO && !bankBounds)
        {
            SetGameManagerVariables();
            return;
        }
        else
        {
            if (!hasSpawnedPlayersAtSpawnPoints)
            {
                SpawnPlayer();
                hasSpawnedPlayersAtSpawnPoints = true;
            }
        }
        


        if (allEnemiesChasePlayer & !playerToChase)
        {
            //Game Ended start another round here!
            SceneManager.LoadScene("Game");
        }


    }
    
    public void SpawnPlayer(GameObject player = null) 
    {
        // Make sure you are running on the indivisual client
        /*if (!view.IsMine)
            return;*/

        //GameObject playerGO = player;



        // If you want to respawn a player, destroy the player first
        if(player)
            PhotonNetwork.Destroy(player);

        // Normal spawning code
        // Instantiate on the network
        spawnedPlayer = PhotonNetwork.Instantiate(GetChosenCharacter(), respawnPoints[Random.Range(0, respawnPoints.Count)].transform.position, Quaternion.identity);

        GameObject newPlayer = spawnedPlayer;
        // Make sure the photon view of the player this client is creating is owned by the same client
        //newPlayer.GetComponent<PhotonView>().TransferOwnership(this.view.ViewID);


        vCam.Follow = newPlayer.transform.Find("PlayerCameraRoot");
        newPlayer.GetComponent<StarterAssets.ThirdPersonController>().SetPlayerMultiplayerName(GetPlayerName());
    }

    private string GetChosenCharacter()
    {
        string name = PlayerPrefs.GetString("Chosen Character");


        if (name.Contains("(Clone)"))
        {
            name = name.Split('(')[0];
        }
        return name;
    }


    private void SetGameManagerVariables()
    {
        FindSpawnPoints();
        FindCamera();
        FindBankBoundsAndMoneyBag();
        SpawnPlayer();
    }
    public void FindSpawnPoints() 
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        respawnPoints.Clear();
        foreach(GameObject spawnPoint in spawnPoints)
        {
            respawnPoints.Add(spawnPoint);
        }
    }

    private void FindBankBoundsAndMoneyBag()
    {

        bankBounds = GameObject.Find("Bank Bounds")?.GetComponent<Collider>();

        moneyBagGO = GameObject.Find("Money Bag");
        moneyBagGO?.SetActive(false);

        heliSpawnPoint = GameObject.Find("HeliSapwnPos")?.transform; 
    }
    public void FindCamera()
    {
        cam = Camera.main;
        vCam = FindObjectOfType<CinemachineVirtualCamera>();
    }

    public string GetPlayerName()
    {
        return PlayerPrefs.GetString("Player Name");
    }

    public void ChangeGameState(GameStates newState)
    {
        string state = newState.ToString();
        view.RPC("ChangeGameStateRPC", RpcTarget.AllBuffered, state);
    }

    [PunRPC]
    private void ChangeGameStateRPC(string gameState)
    {
        
        GameStates newState = (GameStates)System.Enum.Parse(typeof(GameStates), gameState);

        if (newState == GameStates.Chase)
        {
            //code here
            //StartTheChase();
            Debug.Log("CHASE STARTED!!!!!!!!!");
            gameUI.LogMessageInGame("CHASE STARTED!", 5F);

        }
    }

    public void StartTheChase(StarterAssets.ThirdPersonController _playerWithBag)
    {
        view.RPC("StartTheChaseRPC", RpcTarget.AllBuffered, _playerWithBag.GetComponent<PhotonView>()?.ViewID);
    }

    [PunRPC]
    private void StartTheChaseRPC(int playerID)
    {
        //Code goes here
        StarterAssets.ThirdPersonController _playerWithBag = PhotonNetwork.GetPhotonView(playerID).GetComponent<StarterAssets.ThirdPersonController>();
        playerWithBag = _playerWithBag.gameObject;

        string message = "Player with bag is: " + playerWithBag.name;
        gameUI.LogMessageInGame(message, 5F);

        AllEnemiesChasePlayer(_playerWithBag.transform);

        theChaseAlarms.SetActive(true);
        /*EnemyController[] enemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in enemies)
        {
            enemy.SwitchToAttackMode(true, _playerWithBag);
        }*/
        
        /*if (spawnedHelicopter == null)   //make sure we don't spawn more than one helicopter
            SpawnHelicopter();*/
    }

    private void SpawnHelicopter()
    {
        spawnedHelicopter = GameObject.Instantiate(helicopterGO, heliSpawnPoint.position, Quaternion.identity);
    }

    public void DropBag(Vector3 position)
    {
        view.RPC("DropBagRPC", RpcTarget.All, position);
    }

    [PunRPC]
    public void DropBagRPC(Vector3 position)
    {
        //moneyBagGO.SetActive(true);
        Debug.Log("Bag is meant to be dropped over the network");
        SetNetworkObjectActive(moneyBagGO, true);
        position = new Vector3(position.x + dropDistance, position.y + dropDistance, position.z + dropDistance);
        moneyBagGO.transform.position = position;
        moneyBagGO.transform.localScale = Vector3.one;

        allEnemiesChasePlayer = false;
    }

    public void PickBag() 
    {
        view.RPC("PickBagRPC", RpcTarget.All);
    }

    [PunRPC]
    public void PickBagRPC()
    {
        SetNetworkObjectActive(moneyBagGO, false);
    }

    public void EndGame()
    {
        view.RPC("EndGameRPC", RpcTarget.All);
    }

    [PunRPC]
    public void EndGameRPC()
    {
        gameState = GameStates.Finish;
        PhotonNetwork.CurrentRoom.IsOpen = true;

        //Celebrate for 5 seconds
        Invoke("LoadMenu", 5);

        if(view.IsMine && !hasSpawnedRespawner)
            StartCoroutine("HelpWithReturnToMenu");

    }

    public IEnumerator HelpWithReturnToMenu()
    {
        // Helps the menu restart
        GameObject respawner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hasSpawnedRespawner = true;
        respawner.tag = "Respawn";
        Destroy(respawner.GetComponent<MeshRenderer>());
        DontDestroyOnLoad(respawner);
        yield return new WaitForSeconds(10);

    }

    public void LoadMenu()
    {
        PhotonNetwork.LoadLevel(0);
    }

    public void WinGame(StarterAssets.ThirdPersonController playerThatWon) 
    {
        int playerNetworkID = playerThatWon.GetComponent<PhotonView>().ViewID;
        view.RPC("WinGameRPC", RpcTarget.All, playerNetworkID);


        playerThatWon.GetComponent<PhotonView>().RPC("WinOrLoseRPC", RpcTarget.All, playerNetworkID);
    }

    [PunRPC]
    public void WinGameRPC(int viewID)
    {
        StarterAssets.ThirdPersonController playerThatWon = PhotonNetwork.GetPhotonView(viewID).GetComponent<StarterAssets.ThirdPersonController>();  

        gameUI.LogMessageInGame("Player " + playerThatWon.GetPlayerName() + " has Won the Game!", 5f);
        allEnemiesChasePlayer = false;
        EndGame();        
    }

    public void AllEnemiesChasePlayer(Transform _playerToChase) 
    {
        allEnemiesChasePlayer = true;
        playerToChase = _playerToChase;
        
    }



    // Game Network Management
    public void SetNetworkObjectActive(GameObject obj, bool yes)
    {
        PhotonView objView = obj.GetComponent<PhotonView>();
        if (objView != null)
        {
            view.RPC("SetNetworkObjectActiveRPC", RpcTarget.All, objView.ViewID, yes);
        }
        else
        {
            Debug.Log("No Photon view on game object: " + obj.name);
        }
    }

    [PunRPC]
    private void SetNetworkObjectActiveRPC(int viewID, bool yes)
    {
        PhotonNetwork.GetPhotonView(viewID)?.gameObject.SetActive(yes);
        Debug.Log("View Id was: " + viewID);
    }

}
