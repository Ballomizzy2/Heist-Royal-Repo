using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using StarterAssets;
using Photon.Pun;

public class EnemyController : MonoBehaviourPunCallbacks, IPunObservable
{
    private const float SPEED_MULTIPLIER = 0.01f;
    private const float TURN_COLLISION_DISTANCE = 0.3f;
    private float turnDetectionDistance;

    [SerializeField]
    public Enemy.EnemyType type;

    private Enemy myEnemy;
    private enum NPCActivity
    {
        moving,
        turning,
        waiting,
        attacking
    }

    [SerializeField] 
    private NPCActivity currentNpcActivity;

    private Animator enemyAnimator;
    private PhotonView enemyView;

    [SerializeField]
    private bool isMoving, hasStartedMoving, isTurning, isWaiting, isInAttackMode, hasStartedAttacking;

    private float activityTimeThresold,
                  activityTimer;

    private NavMeshAgent navMeshAgent;
    private Vector3 lastDestination;
    private GameManager gameManager;

    [SerializeField]
    private Collider movementBounds;

    private Transform playerTarget;
    [SerializeField]
    private StarterAssets.ThirdPersonController targetPlayerControl;
    [SerializeField]private GameObject playerDetectorCollider;

    [SerializeField]
    private HelicopterController helicopterController;

    GameObject enemyAttackVFX;
    private float heightDetectorVar = 3;

    private void Awake()
    {
        myEnemy = new Enemy(type);
        navMeshAgent = GetComponent<NavMeshAgent>();
        turnDetectionDistance = transform.localScale.z + TURN_COLLISION_DISTANCE;

        float num = myEnemy.GetDetectRadius();
        if (playerDetectorCollider)
        {
            playerDetectorCollider.transform.localScale = new Vector3(num, 1, num); 
        }
        gameManager = FindObjectOfType<GameManager>();
        movementBounds = gameManager.bankBounds;
        helicopterController = GetComponent<HelicopterController>();
        enemyAnimator = GetComponent<Animator>();
        enemyView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        SwitchActivity();
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient && enemyView.IsMine)
            return;

        if(helicopterController != null && helicopterController.GetHasFoundPlayer())
        {
            return;
        }

        if (gameManager.allEnemiesChasePlayer)
        {
            isInAttackMode = true;
            playerTarget = gameManager.playerToChase;
            targetPlayerControl = playerTarget.GetComponent<ThirdPersonController>();
            SwitchToAttackMode(true, targetPlayerControl);
        }
        if (!movementBounds)
        {
            movementBounds = gameManager.bankBounds;
        }
        if (!isInAttackMode)
        {
            activityTimer += Time.deltaTime;
            if (activityTimer >= activityTimeThresold)
            {
                SwitchActivity();
                if(type==Enemy.EnemyType.Helicopter) 
                    activityTimeThresold = Random.Range(0.5f, 2.0f);
                else
                    activityTimeThresold = Random.Range(5.0f, 10.0f);
                activityTimer = 0;
            }
            Act();

            if(type == Enemy.EnemyType.Helicopter)
                isInAttackMode = false;
        }
        else
        {
            if (!playerTarget)
            {
                isInAttackMode = false;
                return;
            }
            Attack(playerTarget);
            AttackPlayer();
            if(gameManager.allEnemiesChasePlayer == false)
                AttemptToLeavePlayer();
        }
    }

    private void SwitchActivity()
    {
        int decision = Random.Range(0, 3);
        switch (decision)
        {
            case 0:
                currentNpcActivity = NPCActivity.moving;
                break;
            case 1:
                currentNpcActivity = NPCActivity.waiting;
                break;
            case 2:
                currentNpcActivity = NPCActivity.turning;
                break;
            default:
                break;
        }
        photonView.RPC("SyncActivity", RpcTarget.Others, (int)currentNpcActivity);
    }

    [PunRPC]
    private void SyncActivity(int activity)
    {
        currentNpcActivity = (NPCActivity)activity;
    }

    private void Act()
    {
        switch (currentNpcActivity)
        {
            case NPCActivity.moving:
                if(!hasStartedMoving)
                    Move();
                break;
            case NPCActivity.turning:
                Turn();
                hasStartedMoving = false;
                break;
            case NPCActivity.waiting:
                Wait();
                hasStartedMoving = false;
                break;
            default:
                break;
        }

        if (!isInAttackMode)
        {
            enemyAnimator.SetBool("Idle", isWaiting);
            enemyAnimator.SetBool("Move", hasStartedMoving);
        }
    }

    private void Move()
    {
        if (currentNpcActivity != NPCActivity.moving)
            return;
        if(type != Enemy.EnemyType.Helicopter)
        {
            Vector3 destination = new Vector3();
      
            isMoving = true;
            isWaiting = false;
            isTurning = false;
            isInAttackMode = false;
            hasStartedAttacking = false;

            float x = Random.Range(movementBounds.bounds.min.x, movementBounds.bounds.max.x);
            float y = Random.Range(movementBounds.bounds.min.y, movementBounds.bounds.max.y);
            float z = Random.Range(movementBounds.bounds.min.z, movementBounds.bounds.max.z);
            destination = new Vector3(x, y, z);
            if(!isInAttackMode)
                navMeshAgent.speed = myEnemy.GetMoveSpeed() * 0.5f;
            navMeshAgent.SetDestination(destination);
            //photonView.RPC("SyncDestination", RpcTarget.Others, destination);
            hasStartedMoving = true;
        }
        else
        {
            transform.position += transform.TransformDirection(0, 0, myEnemy.GetMoveSpeed() * myEnemy.GetAttackSpeed() * Time.deltaTime * SPEED_MULTIPLIER);
            float x1, z1;
            x1 = Mathf.Clamp(transform.position.x, movementBounds.bounds.min.x, movementBounds.bounds.max.x);
            z1 = Mathf.Clamp(transform.position.z, movementBounds.bounds.min.z, movementBounds.bounds.max.z);
            transform.position = new Vector3(x1, transform.position.y, z1);
            //photonView.RPC("SyncPosition", RpcTarget.Others, transform.position);
        }
    }

    /*[PunRPC]
    private void SyncDestination(Vector3 destination)
    {
        navMeshAgent.SetDestination(destination);
    }

    [PunRPC]
    private void SyncPosition(Vector3 position)
    {
        transform.position = position;
    }*/

    private void Turn()
    {
        isTurning = true;
        isWaiting = false;
        isMoving = false;
        isInAttackMode = false;
        hasStartedAttacking = false;

        float angle = Random.Range(0.0f, 360.0f);
        transform.localRotation = Quaternion.Euler(0, angle, 0);
        //photonView.RPC("SyncTurn", RpcTarget.Others, angle);
        currentNpcActivity = NPCActivity.moving;
        Act();
    }

   /* [PunRPC]
    private void SyncTurn(float angle)
    {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }*/

    private void Wait()
    {
        isWaiting = true;
        isMoving = false;
        isTurning = false;
        isInAttackMode = false;
        hasStartedAttacking = false;
        currentNpcActivity = NPCActivity.waiting;

        navMeshAgent.isStopped = true;
    }

    public void SwitchToAttackMode(bool confirm, StarterAssets.ThirdPersonController player)
    {
        if(confirm)
        {
            currentNpcActivity = NPCActivity.attacking;
            targetPlayerControl = player;
            playerTarget = player.transform;
        }
        isInAttackMode = confirm;
        enemyAnimator.SetBool("Idle", false);
        enemyAnimator.SetBool("Move", true);
        enemyAnimator.SetFloat("MoveSpeed", 1);
        photonView.RPC("SyncAttackMode", RpcTarget.Others, confirm, player.photonView.ViewID);
    }

    [PunRPC]
    private void SyncAttackMode(bool confirm, int playerID)
    {
        isInAttackMode = confirm;
        targetPlayerControl = PhotonView.Find(playerID).GetComponent<ThirdPersonController>();
        playerTarget = targetPlayerControl.transform;
    }

    private void Attack(Transform targ)
    {
        if (targetPlayerControl.IsDead())
            return;
        navMeshAgent.speed = myEnemy.GetMoveSpeed() * 0.1f;

        Vector3 directionToTarget = (targ.position - transform.position).normalized;
        Vector3 dest = targ.position - directionToTarget;

        if(dest != lastDestination)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.SetDestination(dest);
            transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));
            lastDestination = dest;
            photonView.RPC("SyncAttack", RpcTarget.Others, dest, transform.rotation);
        }

        hasStartedAttacking = true;

        isMoving = false;
        hasStartedMoving = false;
        isWaiting = false;
        isTurning = false;
        currentNpcActivity = NPCActivity.attacking;
    }

    [PunRPC]
    private void SyncAttack(Vector3 destination, Quaternion rotation)
    {
        navMeshAgent.SetDestination(destination);
        transform.rotation = rotation;
        lastDestination = destination;
    }

    private void AttackPlayer()
    {
        if(playerTarget != null)
        {
            if (Vector3.Distance(playerTarget.position, transform.position) < myEnemy.GetAttackRadius())
            {
                StartCoroutine(AttackAnimation());
            }
        }
    }

    public IEnumerator AttackAnimation()
    {
        enemyAnimator.SetTrigger("Attack");


        //GameObject obj = null;
        if(!enemyAttackVFX) 
            enemyAttackVFX = PhotonNetwork.Instantiate("Enemy Attack Volume", transform.position, Quaternion.identity);
        yield return new WaitForSeconds(1.5f);
        if(enemyAttackVFX != null)
            PhotonNetwork.Destroy(enemyAttackVFX.gameObject);

        Strike();

        navMeshAgent.isStopped = false;
        hasStartedAttacking = false;
        if(playerTarget != null)
        {
            enemyAnimator.SetFloat("MoveSpeed", 1);
            Attack(playerTarget);
        }
        else
            enemyAnimator.SetFloat("MoveSpeed", 0);
    }

    private void Strike()
    {
        Collider[] col = Physics.OverlapSphere(transform.position, myEnemy.GetAttackRadius());

        for (int i = 0; i < col.Length; i++)
        {
            if (col[i].CompareTag("OtherPlayer") || col[i].CompareTag("Player"))
            {
                ThirdPersonController tps = col[i].GetComponent<ThirdPersonController>();
                tps.TakeDamage(myEnemy.GetAttackAmount());
                
                tps.GetComponent<PhotonView>().RPC("ReceiveForceRPC", RpcTarget.All, transform.forward, 5f, tps.photonView.ViewID);
            }
        }
    }

    private void AttemptToLeavePlayer()
    {
        if (playerTarget != null)
        {
            // check if the player is to far on the y axis top and bottom
            if(playerTarget.transform.position.y > transform.position.y + heightDetectorVar || playerTarget.transform.position.y < transform.position.y - heightDetectorVar)
            {
                Debug.Log("YYY");
                isInAttackMode = false;
                hasStartedAttacking = false;
                playerTarget = null;
                SwitchActivity();
                enemyAnimator.SetFloat("MoveSpeed", 0);
                enemyAnimator.ResetTrigger("Attack");
                playerDetectorCollider.GetComponent<PlayerDetector>()?.FlickCollider();
                photonView.RPC("SyncLeavePlayer", RpcTarget.Others);
                return;
            }

            if (Vector3.Distance(playerTarget.position, transform.position) > myEnemy.GetDetectRadius())
            {
                isInAttackMode = false;
                hasStartedAttacking = false;
                playerTarget = null;
                SwitchActivity();
                enemyAnimator.SetFloat("MoveSpeed", 0);
                enemyAnimator.ResetTrigger("Attack");
                photonView.RPC("SyncLeavePlayer", RpcTarget.Others);
                return;
            }

            // player crouches behind something
            if (targetPlayerControl.IsCrouching())
            {
                // make sure the player can see the player
                RaycastHit hit;
                Vector3 origin = transform.position + new Vector3(0, transform.localScale.y / 2, 0);
                if (Physics.Raycast(origin, transform.forward, out hit))
                {
                    if (hit.collider.gameObject != targetPlayerControl.gameObject)
                    {
                        Debug.DrawLine(origin, hit.point);
                        Debug.Log("Line!!!");
                        isInAttackMode = false;
                        hasStartedAttacking = false;
                        playerTarget = null;
                        SwitchActivity();
                        playerDetectorCollider.GetComponent<PlayerDetector>()?.FlickCollider();
                        enemyAnimator.SetFloat("MoveSpeed", 0);
                        enemyAnimator.ResetTrigger("Attack");
                        photonView.RPC("SyncLeavePlayer", RpcTarget.Others);
                        return;
                    }
                }  
            }
            
        }

        
        if (targetPlayerControl.IsDead())
        {
            isInAttackMode = false;
            hasStartedAttacking = false;
            playerTarget = null;
            SwitchActivity();
            enemyAnimator.SetFloat("MoveSpeed", 0);
            enemyAnimator.ResetTrigger("Attack");
            photonView.RPC("SyncLeavePlayer", RpcTarget.Others);
            return;
        }

        
    }

    public bool EnemyFacingPlayer()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + new Vector3(0, transform.localScale.y / 2, 0);
        if (Physics.Raycast(origin, transform.forward, out hit))
            return (hit.collider.gameObject != targetPlayerControl.gameObject);
        return false;
    }

    [PunRPC]
    private void SyncLeavePlayer()
    {
        isInAttackMode = false;
        hasStartedAttacking = false;
        playerTarget = null;
        SwitchActivity();
        enemyAnimator.SetFloat("MoveSpeed", 0);
        enemyAnimator.ResetTrigger("Attack");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isMoving);
            stream.SendNext(isTurning);
            stream.SendNext(isWaiting);
            stream.SendNext(isInAttackMode);
            stream.SendNext(hasStartedAttacking);
        }
        else
        {
            isMoving = (bool)stream.ReceiveNext();
            isTurning = (bool)stream.ReceiveNext();
            isWaiting = (bool)stream.ReceiveNext();
            isInAttackMode = (bool)stream.ReceiveNext();
            hasStartedAttacking = (bool)stream.ReceiveNext();
        }
    }
}
