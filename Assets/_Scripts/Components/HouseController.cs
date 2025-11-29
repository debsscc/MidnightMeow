// /*----------------------------------------------
// ------------------------------------------------
// Creation Date: 2025-11-09 21:13
// Author: Debs S Carvalho
// /*----------------------------------------------
// ----------------------------------------------*/

using UnityEngine;


public class HouseController : MonoBehaviour
{
    //private StateAnimation stateAnimation
    private HealthComponent health;
    private AudioEmitter audioEmitter;
    public static event System.Action OnHouseDestroyed;

    [SerializeField] private VXFEmitter hitEffect;


    private void Awake()
    {
        //stateAnimation = GetComponent<Animation>();
        health = GetComponent<HealthComponent>();
        audioEmitter = GetComponent<AudioEmitter>();

        health.OnDied.AddListener(OnDeath);
    }

    void Update()
    {
        if (!health.IsAlive)
            return;
    }

    public void TakeDamage(float amount)
    {  
        health.TakeDamage(amount, gameObject);
        if (hitEffect != null)
            hitEffect.PlayVFX();
    }

    void OnDeath()
    {
        //stateAnimation.play("DestructionHouse");
        //audioEmitter.PlaySound(destructionClip);

        OnHouseDestroyed?.Invoke();        
    }
}

//No GameManager: void OnEnable() => HouseController.OnHouseDestroyed += HandleGameOver;
// e tb: void OnDisable() => HouseController.OnHouseDestroyed -= HandleGameOver;

// o inimigo = targetHouse.TakeDamage(damage)