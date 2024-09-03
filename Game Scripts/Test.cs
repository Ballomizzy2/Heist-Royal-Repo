using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    //Test Enemy Class

    private float damageAmount = 1000; 
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            collider.GetComponent<StarterAssets.ThirdPersonController>().TakeDamage(damageAmount);
        }
    }
}
