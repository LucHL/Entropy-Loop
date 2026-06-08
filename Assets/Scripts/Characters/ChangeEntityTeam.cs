using System;
using UnityEngine;

public class EntityTeam : MonoBehaviour
{
    public static EntityTeam instance;

    void Awake()
    {
        instance = this;
    }

    public void ChangeTeam(UnitsTeam newTeam)
    {
        if (newTeam == UnitsTeam.Enemy)
            gameObject.tag = "Enemy";
        else
            gameObject.tag = "Champion";
    }
}
