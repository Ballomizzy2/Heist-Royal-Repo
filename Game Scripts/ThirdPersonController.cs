using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
using System;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    //[RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviourPunCallbacks
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 3.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 10.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 30.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        [SerializeField] public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        //Player Testing
        [Header("Player Testing")]
        [SerializeField]
        private bool isPlayerATest = false;


        //Crouching
        [Space]
        [Header("Crouching")]
        [SerializeField]
        private bool isCrouching;
        private const float NORMAL_COLLISION_HEIGHT = 1.8F,
                            CROUCH_COLLISION_HEIGHT = 1.2F;

        private Vector3 NORMAL_COLLISION_CENTER = new Vector3(0, 0.93f, 0),
                        CROUCH_COLLISION_CENTER = new Vector3(0, 0.6f, 0);

        //Chest and Key Management
        [Space]
        [Header("Key Management")]
        [SerializeField]
        private bool hasKey;

        //Pushing Management
        [Space]
        [Header("Push Management")]
        [SerializeField]
        private Vector3 pushDirection;
        [SerializeField]
        private const float pushForce = 1.5f;
        private float fallInactiveDuration; // number of seconds the player is inactive when they fall
        [SerializeField]private bool isToBePushed, waitingForPushReset;

        public bool HasBeenPushed()
        {
            return isToBePushed;
        }

        //Health Management
        [Space]
        [Header("Health")]
        [SerializeField]
        private Health playerHealth;
        private bool isDead;

        [SerializeField] GameObject dieVFX;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        //fallout damage
        private float fallDamageMultiplier = 15f;
        private float fallDamageThresold = -15f;

        //Money Management
        [SerializeField] private bool hasMoney = false;
        [SerializeField] private GameObject moneyBag;
        [SerializeField] private GameObject moneyBagGO;
        VaultController lastVault;


        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDStumbleDirection;
        private int _animIDStumble;

        // game references
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private GameManager _gameManager;
        private GameUIManager _gameUIManager;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        //Multiplayer
        PhotonView view;
        private TextMeshPro _playerNamePlaceHolder;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }




        // KEY BINDINGS
        [Space]
        [SerializeField]
        [Header("Key Bindings")]
        private KeyCode crouchKey = KeyCode.LeftControl;


        // VFX
        [SerializeField]
        private GameObject RunSmoke;


        private void Start()
        {
            view = GetComponent<PhotonView>();

            //This has to happen to every player regardless of Photon View
            //Set money bag prefab
            //moneyBag = GameObject.FindGameObjectWithTag("Bag");


            //Set Money Bag Object to be not active
            moneyBag.tag = "Untagged";
            moneyBag.SetActive(false);

            // Set vfx stuff
            RunSmoke = transform.Find("Run Smoke").gameObject;
            RunSmoke.SetActive(false);
            

            //Create Documentation for Data
            playerHealth = new Health("Player Health", 1000);


            if (!view.IsMine)
            {
                // If it is not the player, destroy anything that can cause player control etc
                Destroy(GetComponent<StarterAssetsInputs>());
                Destroy(GetComponent<PlayerInput>());
                gameObject.tag = "OtherPlayer";
                //Destroy(GetComponent<PlayerManager>());

                return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _gameManager = FindObjectOfType<GameManager>();
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
            _gameUIManager = FindObjectOfType<GameUIManager>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            
        }

        private void Update()
        {
            if (!view.IsMine)
                return;
            

            if (isDead)
                return;

            _hasAnimator = TryGetComponent(out _animator);


            ReceivePush();
            Die();
            GroundedCheck();

            if (isToBePushed)
                return;
                
            Move();
            JumpAndGravity();
            ToggleCrouch();
            RunVFX();
        }

        private void LateUpdate()
        {
            if (!view.IsMine)
                return;

            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            if (!view.IsMine)
                return;

            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDStumbleDirection = Animator.StringToHash("StumbleDirection");
            _animIDStumble = Animator.StringToHash("Stumble");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = _controller.isGrounded;
            // Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator && !isToBePushed)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = (_input.sprint && !isCrouching) ? SprintSpeed : MoveSpeed;            

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                //fall damage
                if(_verticalVelocity < fallDamageThresold)
                {
                    this.TakeDamage(Mathf.Abs(_verticalVelocity) * fallDamageMultiplier);
                    Debug.Log("Fall Damage!!!, " + _verticalVelocity);
                }
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // Stop crouching
                    
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                        DisableCrouch();
                        
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                        DisableCrouch();
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        //Crouch
        private void ToggleCrouch()
        {
            if (Grounded && _input.crouch && !isCrouching)
            {
                //Crouch
                isCrouching = true;
                //Crouch collision update
                _controller.height = CROUCH_COLLISION_HEIGHT;
                _controller.center = CROUCH_COLLISION_CENTER;
            }
            else if (Grounded && _input.crouch && isCrouching)
            {
                //stand
                isCrouching = false;
                _controller.height = NORMAL_COLLISION_HEIGHT;
                _controller.center = NORMAL_COLLISION_CENTER;
            }
            _animator.SetLayerWeight(1, Convert.ToInt32(isCrouching));
            _input.crouch = false;
        }

        private void DisableCrouch()
        {
            //stand
            isCrouching = false;
            _controller.height = NORMAL_COLLISION_HEIGHT;
            _controller.center = NORMAL_COLLISION_CENTER;
            _animator.SetLayerWeight(1, Convert.ToInt32(isCrouching));
        }

        public bool IsCrouching()
        {
            return isCrouching;
        }

        public void TakeDamage(float _damage)
        {
            if (!view.IsMine)
                return;
            Debug.Log("I HAVE BEEN HIT, OH DEAR!! Damage amount: " + _damage + " I have " + playerHealth.CurrentHealthAmount);
            view.RPC("TakeDamageRPC", RpcTarget.AllBuffered, _damage, view.ViewID);
        }

        [PunRPC]
        public void TakeDamageRPC(float _damage, int playerID)
        {
            if(view.ViewID == playerID)
                playerHealth.GetDamage(_damage);
        }

        public void Die()
        {

            if(playerHealth.CurrentHealthAmount <= 0) 
            {
                //Animation e.g. Ragdoll
                _animator.SetTrigger("Die");
                //PhotonNetwork.Instantiate("Player Die FX", transform.position, Quaternion.identity).transform.SetParent(transform);
                // vfx Change Camera or zoom out

                /*RaycastHit hit;
                if(Physics.Raycast(transform.position - new Vector3(0, transform.localScale.y/2, 0), Vector3.down, out hit))
                {
                    Debug.DrawLine(transform.position, hit.point);
                    transform.position = hit.point;
                }*/

                Debug.Log("Oh Dear, I died!");
                isDead = true;

                //Drop Money
                if (hasMoney)
                {
                    GetMoney(false);
                    _gameManager.DropBag(transform.position);
                }

                // Adjust collider
                CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
                capsuleCollider.height = .5f;
                capsuleCollider.center = new Vector3(0, .5f, 0);

                //Send data across network
                view.RPC("DieRPC", RpcTarget.AllBuffered, view.ViewID);

                //Respawn
                Invoke("Respawn", 5);

            }
        }

        [PunRPC]
        public void DieRPC(int playerID)
        {
            if(view.ViewID == playerID)
            {
                //_animator.SetTrigger("Die");
                isDead = true;

                //Drop Money
                if (hasMoney)
                {
                    GetMoney(false);
                    _gameManager.DropBag(transform.position);
                }


                //Respawn
                //Invoke("Respawn", 5);
            }
        }

        public bool IsDead()
        {
            return isDead;
        }

        private void Respawn()
        {
            if (!view.IsMine)
                return;
            _gameManager.SpawnPlayer(this.gameObject);

            //view.RPC("RespawnRPC", RpcTarget.OthersBuffered, view.ViewID);
        }


        [PunRPC]
        public void ReceivePushRPC(Vector3 pushDir, int playerPushed)
        {
            Debug.Log("Push is meant to be received on view " + view.ViewID + " but is received on " + playerPushed);
            if (playerPushed == view.ViewID)
            {
                Debug.Log("I am the player to be pushed");
                ReceivePush(pushDir);
            }
        }

        public void ReceivePush(Vector3 _pushDirection)
        {
            if (!view.IsMine)
                return;

            Debug.Log("I was pushed");

            //If player has bag on them, they fall and the bag falls
            isToBePushed = true;
            pushDirection = _pushDirection;
            //Force acts on _pushdirection
            //controllerToPush.Move(_pushDirection * pushForce);
        }

        public void ReceivePush() 
        {

            if (isToBePushed && !waitingForPushReset)
            {
                int dir = 0;
                // let player fall
                if ((transform.forward.z + pushDirection.z) <= 0.99f) // push is coming from front
                {
                    dir = -1;
                    transform.forward = -pushDirection;
                    fallInactiveDuration = 8.5f;
                }
                else
                {
                    dir = 1;
                    transform.forward = pushDirection;
                    fallInactiveDuration = 22f;
                }           
                
                _animator.SetFloat(_animIDStumbleDirection, dir);
                _animator.SetTrigger(_animIDStumble);
                _controller.SimpleMove(pushDirection * 10f);
                //isToBePushed = false;
                Vector3 move = pushDirection.normalized;
                //_controller.SimpleMove(move * pushForce);
                Debug.Log("I am moving " + move * pushForce);

                if (hasMoney)
                {
                    GetMoney(false);
                    _gameManager.DropBag(transform.position);
                }
                
                StartCoroutine(Wait(fallInactiveDuration));
            }

        }

        [PunRPC]
        public void ReceiveForceRPC(Vector3 dir, float force, int playerID)
        {
            if(view.ViewID == playerID)
                ReceiveForce(dir, force);
        }
        private void ReceiveForce(Vector3 dir, float force)
        {
            _controller?.SimpleMove(dir*force);
        }
        private IEnumerator Wait(float duration) 
        {
            waitingForPushReset = true;
            yield return new WaitForSeconds(duration);
            waitingForPushReset = false;
            isToBePushed = false;
        }
        public void SetPlayerMultiplayerName(string name) 
        {
            _playerNamePlaceHolder = transform.Find("PlayerName").GetComponent<TextMeshPro>();
            _playerNamePlaceHolder.SetText(name);
        }
        public string GetPlayerName()
        {
            return _playerNamePlaceHolder.text;
        }

        [PunRPC]
        public void WinOrLoseRPC(int viewID)
        {
            if(viewID == view.ViewID)
            {
                // WIN TEXT
                _gameUIManager.LogMessageInGame("You win", 5f);
            }

            else
            {
                // LOSE TEXT
                _gameUIManager.LogMessageInGame("You LOSE, WOMP WOMP", 5f);
            }
        }






        public void GetMoney(bool var)
        {
            if (!view.IsMine)
                return;

            hasMoney = var;
            //moneyBag.SetActive(var);
            _gameManager.SetNetworkObjectActive(moneyBag, var);

            // sets the value of the hold bag layer to 0 (false) or 1 (true)
            _animator.SetLayerWeight(2, Convert.ToInt32(var));
            _animator.SetBool("HoldBag", var);
            if (var)
            {
                _gameManager.ChangeGameState(GameManager.GameStates.Chase);
                _gameManager.StartTheChase(this);
                //Animate getting money bag
            }
        }
        public void GetKey()
        {
            if (!view.IsMine)
                return;
            // other key thinga
            hasKey = true;
        }


        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (!view.IsMine)
                return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (!view.IsMine)
                return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (view && !view.IsMine)
                return;

            if (!_input)
                return;

            if (isToBePushed || isDead)
                return;

            /*// Make sure we are pointing at what we want to interact with
            RaycastHit hit;

            float dist = Vector3.Distance(_mainCamera.transform.position, this.transform.position);
            Ray ray = new Ray(_mainCamera.transform.position + _mainCamera.transform.TransformDirection(_mainCamera.transform.forward * dist), _mainCamera.transform.forward);

            int characterLayer = LayerMask.NameToLayer("Character");
            int layerMask = ~(1 << characterLayer);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                Debug.Log("Hit is " + hit.collider.name);
                if (hit.collider != other)
                    return;
            else
                return;
            }*/





            //Opening Chest
            if (other.CompareTag("Chest") && _input.interact == true)
            {
                other.GetComponent<ChestController>().OpenChest(this);
                _input.interact = false;
            }

            //Opening Door
            else if (other.CompareTag("Door") && _input.interact == true)
            {
                _animator.SetTrigger("Push");
                DoorController doorController = other.GetComponent<DoorController>();

                // If door is opened, don't open anymore
                if (doorController.IsOpen())
                {
                    _gameUIManager.LogMessageInGame("Door is already opened", 3);
                    return;
                }
                
                if (this.hasKey)
                {
                    other.GetComponent<DoorController>().OpenDoor();
                    _gameUIManager.LogMessageInGame("You have used your key boy!", 3);
                    //hasKey = false;

                }
                else
                    _gameUIManager.LogMessageInGame("You don't have a key to open the door!", 3);
                    _input.interact = false;
            }

            //Opening Vault
            else if (other.CompareTag("Vault"))
            {
                if(_input.interact == true)
                {
                    VaultController ctrl = other.GetComponent<VaultController>();
                    lastVault = ctrl;
                    ctrl.LockPickVault(this);
                    _input.interact = false;
                }
                if (isDead || isToBePushed)
                    lastVault.CancelLockPickVault();
            }

            //Picking Bag
            else if (other.CompareTag("Bag") && _input.interact == true)
            {
               
                GetMoney(true);
                _gameManager.PickBag();
                _input.interact = false;
            }

            //Finish Game/ Escape Island
            else if (other.CompareTag("Finish"))
            {
                if (hasMoney)
                {
                    _gameManager.WinGame(this);
                    _input.interact = false;
                    //_input = null;
                }
                else
                {
                    Debug.Log("You can't leave the island until you have the money");
                }
            }

            //Pushing Another Player
            else if (other.CompareTag("OtherPlayer") && _input.interact == true)
            {

                // make sure the player is in a reasonable distance on the y axis to push, we do't want to oush a player that is standing on us
                bool notPushable = (other.transform.position.y > (transform.position.y + 2)) || (other.transform.position.y < (transform.position.y - 2));
                if (notPushable)
                {
                    Debug.Log("Not pushable cause of y distance");
                    return;
                }

                StarterAssets.ThirdPersonController ctrl = other.GetComponent<StarterAssets.ThirdPersonController>();
                //Before you can push a player, the player must not be on the floor or must have stood up from the floor
                if (ctrl.isToBePushed == false) 
                {
                    // push
                    _animator.SetTrigger("Push");
                    PhotonView pView = other.GetComponent<PhotonView>();
                    pView.RPC("ReceivePushRPC", RpcTarget.All, transform.forward, ctrl.view.ViewID);
                    _gameUIManager.LogMessageInGame( "I am pushing view ID " + ctrl.view.ViewID + " in direction " + transform.forward, 3);
                    _input.interact = false;
                }
                 
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!view.IsMine)
                return;

            if (other.CompareTag("Vault"))
            {
                other.GetComponent<VaultController>().CancelLockPickVault();
            }
        }

        public void RunVFX()
        {
            if(Grounded && !isCrouching)
                RunSmoke.SetActive(_input.sprint);
            else
                RunSmoke.SetActive(false);
        }

        private void DestroyObject(PhotonView viewToDestroy)
        {
            PhotonNetwork.Destroy(viewToDestroy);
        }


    }
}