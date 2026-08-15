using System;
using UnityEngine;

[Serializable]
public class UnitSpeed
{
    private float moveSpeed;

    public float MoveSpeed => moveSpeed;

    public UnitSpeed(float moveSpeed)
    {
        SetMoveSpeed(moveSpeed);
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = Mathf.Max(0f, moveSpeed);
    }   
}