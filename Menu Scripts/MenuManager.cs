using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviourPunCallbacks
{
    // References to other objects and controllers
    NetworkController networkController;
    PhotonView view;

    // references to UI Input fields
    [SerializeField]
    private TMP_InputField playerName, serverName;

    // Room details
    [SerializeField]
    private TextMeshProUGUI playerCount, roomName;

    // number of players in the room
    int noOfPlayersInRoom;

    // UI Groups
    [SerializeField] private GameObject lobbyUI, roomUI, startGameUI;
    [SerializeField] private TextMeshProUGUI networkLogger;

    // Connectivity
    private bool connectedToInternet;
    
    // random player names
    string[] names = {"Emily", "James", "Olivia", "Michael", "Sophia", "Benjamin", "Ava", "William", "Isabella", "Lucas"};


    // checks if player has joined a room
    bool hasJoinedRoom;
    const int MAX_PLAYER_PER_ROOM = 4,
              MIN_PLAYER_PER_ROOM = 1;


    // character selecting
  
    [System.Serializable]
    private struct Character
    {
        public string name;
        public GameObject gameObject;
    }
    [Space]
    [Header("Character Changing")]
    [SerializeField] private List<Character> characters = new List<Character>();
    [SerializeField] private GameObject chosenCharacter;
    [SerializeField] private TMP_Dropdown characterDropdown;

    // Arranging players in room
    [Space]
    [Header("Arranging Characters")]
    [SerializeField]
    private List<Transform> charactersInRoom = new List<Transform>();
    [SerializeField]
    private List<Transform> playerDisplayPositions = new List<Transform>();
    private Vector3 displayPosition;


    // make sure there is only one menu manager
    private static MenuManager instace;


    private void Start()
    {
        //PlayerPrefs.DeleteAll();
        if (instace == null)
        {
            instace = this;
        }

        else
            PhotonNetwork.Destroy(this.gameObject);
        view = GetComponent<PhotonView>();

        // Set network controller
        networkController =  FindObjectOfType<NetworkController>();

        // set text in UI
        playerName.text = (names[Random.Range(0, names.Length)]);
        serverName.text = "Server 1";

        connectedToInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // check if you just finished a round, if so spawn players in right positions etc
       /* if (PlayerPrefs.GetString("JustFinishedRound") == "True")
        {
            // Joined Room stuff
            hasJoinedRoom = true;
            displayPosition = playerDisplayPositions[PhotonNetwork.PlayerList.Length - 1].position + Vector3.up * 0.22f;


            InitiateCharacterSystem();
            StorePlayerName(playerName.text);

            //JoinRoom();

        }
        PlayerPrefs.SetString("JustFinishedRound", "False");*/

    }

    public void CreateRoom()
    {
        string roomName = serverName.text;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4; // Maximum number of players

        if (IsRoomNameValid(roomName))
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        else
            LogText("Room name is not valid", Color.red, 2f);
    }

    public void JoinRoom()
    {       
        string roomName = serverName.text;
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeaveRoom()
    {
        view.RPC("ResetPlayerPosition", RpcTarget.All);
        PhotonNetwork.LeaveRoom();
    }

    [PunRPC]
    private void StartGameRPC()
    {
        // Start Game, Hurray we are done with menu stuff

        // Make sure to store character stuff before starting game
        StoreChosenCharacter();

        // No other people can join room
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(1);
    }
    public void StartGame()
    {
        view.RPC("StartGameRPC", RpcTarget.All);
    }

    public override void OnJoinedRoom()
    {
        string log = "Succesfully joined room " + PhotonNetwork.CurrentRoom.Name;
        LogText(log, Color.green, 2f);
        // Joined Room stuff
        hasJoinedRoom = true;
        displayPosition = playerDisplayPositions[PhotonNetwork.PlayerList.Length - 1].position + Vector3.up * 0.22f;


        InitiateCharacterSystem();
        StorePlayerName(playerName.text);

        //ArrangePlayersInRoom();
        //view.RPC("ArrangePlayersInRoom", RpcTarget.OthersBuffered);
    }

    public override void OnLeftRoom()
    {
        string log = "Succesfully left room";
        LogText(log, Color.green, 2f);
        hasJoinedRoom = false;
    }

    public override void OnPlayerLeftRoom(Player player)
    {
        view.RPC("ResetPlayerPosition", RpcTarget.All);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.Log(message);
        string log = message;
        LogText(log, Color.red, 2f);
    }

    public override void OnJoinRoomFailed(short returnCode, string message) 
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.Log(message);
        string log = message;
        LogText(log, Color.red, 2f);
    }

    
    private void Update()
    {
        if(!connectedToInternet)
        {
            // check if the app is connected to the internet
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable:
                    LogText("No internet connection", Color.red, -1);
                    connectedToInternet = false;
                    break;
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    LogText("Connected via carrier data network!", Color.green, 3f);
                    connectedToInternet = true;
                    SceneManager.LoadScene(0); // reload scene once connected
                    break;
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    LogText("Connected via WiFi!", Color.green, 3f);
                    connectedToInternet = true;
                    SceneManager.LoadScene(0); // reload scene once connected
                    break;
            }
        }

        if (!networkController.hasJoinedLobby && !hasJoinedRoom)
        {
            lobbyUI.SetActive(false);
            roomUI.SetActive(false);
            startGameUI.SetActive(false);
            return;
        }
        

        if (hasJoinedRoom)
        {
            roomUI.SetActive(true);
            if(chosenCharacter)chosenCharacter.SetActive(true);

            lobbyUI.SetActive(false);
            
            // if we have more than the minimum player connected, you can start the game
            startGameUI.SetActive((PhotonNetwork.PlayerList.Length >= MIN_PLAYER_PER_ROOM));



            noOfPlayersInRoom = PhotonNetwork.PlayerList.Length;
            playerCount.text = "Players in Room: " + noOfPlayersInRoom.ToString();
            roomName.text = "Room Name: " + PhotonNetwork.CurrentRoom.Name;
        }
        else
        {
            roomUI.SetActive(false);
            startGameUI.SetActive(false);

            if (chosenCharacter) chosenCharacter.SetActive(false);

            lobbyUI.SetActive(true);
        }
    }


    private void LateUpdate()
    {
        if(noOfPlayersInRoom != PhotonNetwork.PlayerList.Length)
        {
            // A player has mysteriously disconnected
            view.RPC("ResetPlayerPosition", RpcTarget.All);
        }
    }

    //CHARACTER CHANGING and HANDLING
    public void InitiateCharacterSystem()
    {
        characterDropdown.ClearOptions();
        foreach(Character character in characters)
        {
            characterDropdown.options.Add(new TMP_Dropdown.OptionData(character.name));
        }
        SpawnCharacter();
    }
    public void SpawnCharacter()
    {
        string characterName = characterDropdown.options[characterDropdown.value].text;
        foreach (Character character in characters)
        {
            if(characterName == character.name)
            {
                if(chosenCharacter)PhotonNetwork.Destroy(chosenCharacter);
                chosenCharacter = PhotonNetwork.Instantiate(character.name, displayPosition, Quaternion.identity);
            }
        }
    }
    


    // STORING PLAYER NAMES
    private void StorePlayerName(string name)
    {
        PlayerPrefs.SetString("Player Name", name);
    }

    private void StoreChosenCharacter()
    {
        PlayerPrefs.SetString("Chosen Character", chosenCharacter.gameObject.name);
    }

    // reset player variables incase a player disconnects or leaves etc.
    [PunRPC]
    private void ResetPlayerPosition()
    {
        if (chosenCharacter == null)
            return;
        noOfPlayersInRoom = PhotonNetwork.PlayerList.Length;
        displayPosition = playerDisplayPositions[PhotonNetwork.PlayerList.Length - 1].position + Vector3.up * 0.22f;
        chosenCharacter.transform.position = displayPosition;
        Debug.Log(displayPosition);
    }
    

    //MISCELLANEOUS FUNCTIONS


    // check for invalid names for servers
    private bool IsRoomNameValid(string _name)
    {
        bool invalidName = false;
        char space = ' ';
        // check if all letters are just space
        for (int i = 0; i < _name.Length; i++)
        {
            // if player decides to input just spaces, it is invalid
            if (!(_name[i] == space))
                break;
            invalidName = true;
        }
        if (invalidName) return false;
        else return true;
    }

    // use this to print errors to user
    public void LogText(string log, Color color, float seconds)
    {
        networkLogger.color = color;
        networkLogger.text = log;

        if(seconds != -1) // if seconds is -1, leave text visible until the next log
            StartCoroutine(LogTextIE(seconds));
    }

    private IEnumerator LogTextIE(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        networkLogger.color = Color.white;
        networkLogger.text = "";
    }


}
