﻿using UnityEngine;
using System.Collections;
using Crab;
using Crab.Controllers;

public class EnemyController : FactionController
{
    void EnterCombat(Entity target) {
        Debug.Log("Attack!");
    }
    void JustDead(Entity killer) { }
    void JustKilled(Entity victim) { }
    protected override void AnyDamage(int damage, Entity damageCauser, DamageType damageType) { }
}