using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemColor : Item
{
    [SerializeField] private Color color;
    [SerializeField] MeshRenderer meshRenderer;

    protected override void Initialize()
    {
        meshRenderer.material.color = color;
        base.Initialize();
    }

    protected override void UpdatePosition()
    {

        base.UpdatePosition();
    }
}