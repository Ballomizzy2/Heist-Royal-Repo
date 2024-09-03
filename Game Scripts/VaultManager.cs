using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class VaultManager : MonoBehaviour
{

    [SerializeField]
    List<VaultController> vaults = new List<VaultController> ();
    PhotonView view;
    GameUIManager gameUIManager;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        gameUIManager = FindObjectOfType<GameUIManager>();
    }



    private void Start()
    {
        VaultController[] vs = FindObjectsOfType<VaultController>();
        foreach (VaultController v in vs)
        {
            vaults.Add (v);
        }


        // Make the master client choose the vault with money
        if (PhotonNetwork.IsMasterClient)
        {
            int rand = Random.Range(0, vaults.Count);
            Debug.Log(" This is the vault with money " + rand);
            for(int i = 0; i < vaults.Count; i++)
            {
                if (i == rand)
                    vaults[i].SetHasMoney(true);
                else
                    vaults[i].SetHasMoney(false);
            }
            view.RPC("SetVaultWithMoneyRPC", RpcTarget.All, rand);
        }
    }

    [PunRPC]
    public void SetVaultWithMoneyRPC(int vaultWithMoneyIndex)
    {
        for (int i = 0; i < vaults.Count; i++)
        {
            if (i == vaultWithMoneyIndex)
            {
                vaults[i].SetHasMoney(true);
                gameUIManager.LogMessageInGame("This is the vault index: " + vaultWithMoneyIndex, 5);
            }
            else
                vaults[i].SetHasMoney(false);
        }
    }

    public void EmptyAllVaultsOnServer()
    {
        view.RPC("EmptyAllVaultsOnServerRPC", RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void EmptyAllVaultsOnServerRPC()
    {
        for(int i = 0; i < vaults.Count; ++i)
        {
            vaults[i].SetHasMoney(false);
        }
    }
}
