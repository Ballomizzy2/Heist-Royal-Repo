using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class VaultController : MonoBehaviour
{
    /*
     0. There is a Vault Manager that controls all the Vault Controllers and chooses the 'one' vault that has the money ---done
     1. Player interacts with vault at the right spot, the opening place/door of vault ---done
     2. Once you interact with it, the UI opens up ---done
     3. The UI game opens up, and the controller of the UI works ---done
     4. Once the UI returns a true boolean to 'this' vault controller --done
     5. The vault opens ---done
        6. If the vault is the "one" vault
            7. They obtain the money ---DONE
                8. Get the bag, THE CHASE starts! --DONE
        8. Else, 
            9. the vault remains open and a text is shown "Nothing in here"     
     */

    [SerializeField]
    private GameObject lockPickingUI;
    [SerializeField]
    private LockPickingController lockPickingController;

    private StarterAssets.ThirdPersonController player;

    private VaultManager vaultManager;
    private GameManager gameManager;

    [SerializeField]
    private bool hasMoney = false;
    private bool isVaultOpen = false;

    //Animation\
    [SerializeField]
    private Animator vaultDoorAnimator;

    
    private PhotonView vaultView;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        vaultManager = FindObjectOfType<VaultManager>();
        vaultView = GetComponent<PhotonView>();
    }
    public void SetHasMoney(bool has)
    {
        hasMoney = has;
    }

    public void LockPickVault(StarterAssets.ThirdPersonController _player) 
    {
        //Lock pick vault
        Debug.Log("Lock-pick vault");
        lockPickingUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        player = _player;
    }

    public void CancelLockPickVault()
    {
        Debug.Log("Lock-pick vault");
        lockPickingController.EndGame();
        lockPickingUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        player = null;
    }

    public void OpenVault() 
    {
        //Open vault
        Debug.Log("Vault opens");
        //Destroy(gameObject);

        // gameObject.SetActive(false);
        //gameManager.SetNetworkObjectActive(this.gameObject, false);
        vaultDoorAnimator.SetTrigger("Open");


        Cursor.lockState = CursorLockMode.Locked;

        isVaultOpen = true;

        OnVaultOpened();
        vaultView.RPC("OpenVaultRPC", RpcTarget.AllBuffered, vaultView.ViewID);
    }

    [PunRPC]
    public void OpenVaultRPC(int playerID)
    {
        if(vaultView.ViewID == playerID)
        {
            vaultDoorAnimator.SetTrigger("Open");
            isVaultOpen = true;

            //OnVaultOpened();
        }
    }

    private void OnVaultOpened() 
    {
        if (hasMoney)
        {
            player.GetMoney(true);
            vaultManager.EmptyAllVaultsOnServer();
        }
        else
            Debug.Log("Nothing in here :(");
    }

}
