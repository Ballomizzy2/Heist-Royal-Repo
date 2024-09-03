using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy
{
    [System.Serializable]
    public enum EnemyType
    {
        Guard, K9, Helicopter
    }
    private EnemyType enemyType;
    private float moveSpeed, 
                  attackSpeed,
                  attackAmount, 
                  detectRadius,
                  attackRadius;
    
    public Enemy(EnemyType enemyType)
    {
        this.enemyType = enemyType;
        /*this.moveSpeed = moveSpeed;
        this.attackAmount = attackAmount;
        this.enemyHealth = health;*/
        switch (enemyType) 
        {
            case EnemyType.Guard:
                moveSpeed = 2f;
                attackAmount = 100f;
                detectRadius = 20f;
                attackRadius = 5f;
                break;
            case EnemyType.K9:
                moveSpeed = 3f;
                attackAmount = 300f;
                detectRadius = 20f;
                attackRadius = 3f;
                break;
            case EnemyType.Helicopter:
                moveSpeed = 5f;
                attackAmount = 300f;
                detectRadius = 100f;
                attackRadius = 100f;
                break;
            default:
                break;
        }
        attackSpeed = 1.1f;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public float GetAttackAmount()
    {
        return attackAmount;
    }

    public float GetAttackSpeed()
    {
        return attackAmount;
    }

    public float GetDetectRadius()
    {
        return detectRadius;
    }

    public float GetAttackRadius()
    {
        return attackRadius;
    }
    public void SetAttackSpeed(float newAttackSpeed)
    {
        attackSpeed = newAttackSpeed;
    }

    /*
     * Guard
     move Speed  = 2
     attack speed = 1.5f
     attack amount = 100
   
     k9
        move speed = 7
        attack speed = 1.5
        attack amount = 300

     Helicopter
         speed = infinity  
     
     
     */

}
