using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemColor : PlantGrowth
{
    [SerializeField] private Color Color;
    [SerializeField] MeshRenderer MeshRenderer;

    protected override void Initialize()
    {
        MeshRenderer.material.color = Color;
        base.Initialize();
    }

    protected override void UpdatePosition()
    {

        base.UpdatePosition();
    }
}