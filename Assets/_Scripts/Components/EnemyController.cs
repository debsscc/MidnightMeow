// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-09 20:00
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    //private AIMovementComponent ai;
    //private AttackComponent attack;
    private HealthComponent health;
    //private StateAnimationComponent anim;
    private AudioEmitter audioEmitter;

    private void Awake()
    {
        //ai = GetComponent<AIMovementComponent>();
        //attack = GetComponent<AttackComponent>();
        health = GetComponent<HealthComponent>();
        //anim = GetComponent<StateAnimationComponent>();
        audioEmitter = GetComponent<AudioEmitter>();

        health.OnDied.AddListener(OnDeath);
        //quando for events, chamar addListener 
    }

    void Update()
    {
        if (!health.IsAlive)
            return;

        //ai.MoveTowardsTarget();
        //attack.TryAttackTarget();
    }

    void OnDeath()
    {
        //anim.play("Death");
        //audioEmitter.PlaySound(deathClip);
        Destroy(gameObject, 2f);
    }

}

