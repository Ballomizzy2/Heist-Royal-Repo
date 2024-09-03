using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockPickingController : MonoBehaviour
{

    private bool hasPickedLock;
    private VaultController vault;
    private GameManager gameManager;

    // Game Mechanics
    [SerializeField] private GameObject gear1, gear2, gear1Hint, gear2Hint;
    private bool isLocked = true, passedLevel1, passedLevelel2;
    private float gear1Speed = 20f,
                  gear2Speed = 30f,
                  gearAngle = 5f,
                  matchAccuracy = 10f;

    private bool startGame = true, check = false, 
                 clicked = false;

    private float initialGear1Rot, initialGear2Rot;
    [SerializeField]
    private Animator gearAnimator, vaultAnimator;

    private void Awake()
    {
        vault = GetComponentInParent<VaultController>();
        gameManager = FindObjectOfType<GameManager>();

        initialGear1Rot = gear1.transform.localEulerAngles.z;
        initialGear2Rot = gear2.transform.localEulerAngles.z;

        StartGame();
    }

    private void Update()
    {
        //StartGame();

        

        if (isLocked)
        {
            Debug.Log("Clicked: " + clicked);
            // level 1
            
            if (passedLevel1 == false && passedLevelel2 == false)
            {
                gear1Hint.SetActive(true);
                gear2Hint.SetActive(false);
                vaultAnimator.enabled = false;

                // gear 1 rotates till clicked
                if(!clicked && !check)
                    gear1.transform.Rotate(Vector3.forward * gear1Speed * gearAngle * Time.deltaTime);
                // if click does not meet rotation details (modulo), reset to level 1
                if (clicked)
                {
                    check = true;
                    float gear1Rot = gear1.transform.localEulerAngles.z;
                    float gear1HintRot = gear1Hint.transform.localEulerAngles.z;
                    float match = Mathf.Abs(gear1Rot) % 60;
                    if (!(match > -matchAccuracy && match < matchAccuracy)) // does not match
                    {
                        // play sound
                        gearAnimator.SetTrigger("Wrong");
                        passedLevel1 = false;
                        passedLevelel2 = false;
                        check = false;
                        Debug.Log("Wrong)");
                        //return;
                    }
                    // else go to level 2
                    else
                    {
                        check = false;
                        passedLevel1 = true;
                        passedLevelel2 = false;
                        Debug.Log("Correctttt");
                        clicked = false;
                        gear1.transform.localEulerAngles = gear1Hint.transform.localEulerAngles;
                        //return;
                    }
                }

            }
            else if(passedLevel1 == true && passedLevelel2 == false)
            {
                // rotate gear 2 till clicked
                gear1Hint.SetActive(false);
                gear2Hint.SetActive(true);

                // gear 2 rotates till clicked
                if(!clicked && !check)
                    gear2.transform.Rotate(Vector3.forward * gear2Speed * gearAngle * Time.deltaTime);
                // if click does not meet rotation details (modulo), reset to level 1
                if (clicked)
                {
                    check = true;
                    float gear2Rot = gear2.transform.localEulerAngles.z;
                    float gear2HintRot = gear2Hint.transform.localEulerAngles.z;
                    float match = Mathf.Abs(gear2Rot) % 90;
                    if (!(match > -matchAccuracy && match < matchAccuracy)) // does not match
                    {
                        // play sound
                        gearAnimator.SetTrigger("Wrong");
                        passedLevel1 = true;
                        passedLevelel2 = false;
                        check = false;
                        Debug.Log("Wrong 2");
                        //Invoke("");
                        //return;
                    }
                    // else win
                    else
                    {
                        Debug.Log("Correct 2");
                        passedLevel1 = true;
                        passedLevelel2 = true;
                        vaultAnimator.enabled = true;
                        check = false;
                        gear2.transform.localEulerAngles = gear2Hint.transform.localEulerAngles;
                        // sound
                        PickLock();
                    }
                }

            }
                // if click does not match gear 2 hint rotation, reset to level 1
                // else unlock!
        }
    }

    public void Click()
    {
        clicked = true;
        StartCoroutine(ResetClick());
    }

    private IEnumerator ResetClick()
    {
        yield return new WaitForSeconds(1f);
        clicked = false;
    }

    private void StartGame() 
    {
        Vector3 t = gear1.transform.localEulerAngles;
        gear1.transform.localEulerAngles = new Vector3(t.x, t.y, initialGear1Rot);

        t = gear2.transform.localEulerAngles;
        gear2.transform.localEulerAngles = new Vector3(t.x, t.y, initialGear2Rot);


        //if picked vault
        //for now, we would do it with a button tap



        //PickLock();
       

        //if not picked vault properly
        //EndGame();

        //if press "Esc"
        //EndGame();
       
    }


    public void PickLock() 
    {
        hasPickedLock = true;
        isLocked = false;
        Debug.Log("You have succesfully picked the lock!");
        
        this.gameObject.SetActive(false);
        //gameManager.SetNetworkObjectActive(this.gameObject, false);

        vault.OpenVault();
    }

    public void EndGame()
    {
        hasPickedLock = false;
        passedLevel1 = passedLevelel2 = false;
        gear1Hint.SetActive(false);
        gear2Hint.SetActive(false);
        this.gameObject.SetActive(false);
    }
}
