using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    private EnemyController enemyController;
    private HelicopterController helicopterController;
    [SerializeField]
    private bool isHelicopter;

    private SphereCollider sphereCollider;

    private float heightDetectorVar = 3;

    private void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
        helicopterController= GetComponentInParent<HelicopterController>();
        sphereCollider = GetComponent<SphereCollider>();

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(isHelicopter)
                helicopterController.FoundPlayer(other.GetComponent<StarterAssets.ThirdPersonController>());
            else
                enemyController.SwitchToAttackMode(true, other.GetComponent<StarterAssets.ThirdPersonController>());
        }
    }

    public void FlickCollider()
    {
        StartCoroutine(ColliderFlickerIE());
    }

    private IEnumerator ColliderFlickerIE()
    {
        sphereCollider.enabled = false;
        yield return new WaitForSeconds(1f);
        sphereCollider.enabled = true;
    }
}
