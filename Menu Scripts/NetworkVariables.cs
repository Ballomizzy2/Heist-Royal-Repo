using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkVariables : MonoBehaviour
{
    public struct NetworkVariable
    {
        public string value;
        public ulong id;
    };

    private List<NetworkVariable> variables = new List<NetworkVariable>();


    public void StoreVar(string value, ulong id)
    {
        NetworkVariable var = new NetworkVariable();
        var.value = value;
        var.id = id;
        variables.Add(var);
    }

    public string GetVar(ulong id)
    {
        foreach (NetworkVariable var in variables)
        {
            if (var.id == id)
            {
                return var.value;
            }
        }
        return null;
    }

    //Player Name
    private string playerName;

    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    public string GetPlayerName()
    {
        return playerName;
    }
}
