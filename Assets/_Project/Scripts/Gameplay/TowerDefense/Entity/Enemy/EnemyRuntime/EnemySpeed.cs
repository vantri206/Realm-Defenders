using System;
using UnityEngine;

[Serializable]
public class EnemySpeed
{
    private float moveSpeed;

    public float MoveSpeed => moveSpeed;

    public EnemySpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = newMoveSpeed;
    }
}