using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DoorController : MonoBehaviour
{
    [SerializeField]
    private GameObject doorHinge, directionHelper;
    private bool isLocked = true, isOpened;
    PhotonView view;

    private void Start()
    {
        view = GetComponent<PhotonView>();
    }

    public void OpenDoor()
    {
        view.RPC("OpenDoorRPC", RpcTarget.All, view.ViewID);
    }

    [PunRPC]
    public void OpenDoorRPC(int viewID)
    {
        if (viewID != view.ViewID)
            return;

        if (isLocked)
        {
            //Physically open the door
            Invoke("OpenAnim", 0.5f);
            isLocked = false;
            isOpened = true;

            //vfx and sfx

        }
    }

    


    private void OpenAnim()
    {
        doorHinge.transform.Rotate(0, -90, 0);
    }

    public bool IsOpen()
    {
        return isOpened;
    }
    public void CloseDoor()
    {
        isLocked = true;
        isOpened = false;
        //Close door after a few seconds
    }
}
