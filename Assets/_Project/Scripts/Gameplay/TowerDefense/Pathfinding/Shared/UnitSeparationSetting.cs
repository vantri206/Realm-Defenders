using System;
using UnityEngine;

[Serializable]
public class UnitSeparationSettings
{
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float weight = 0.45f;
    [SerializeField] private float maxForce = 0.8f;
    [SerializeField] private int cellSearchRange = 1;

    public float Radius => radius;
    public float Weight => weight;
    public float MaxForce => maxForce;
    public int CellSearchRadius => cellSearchRange;
}
