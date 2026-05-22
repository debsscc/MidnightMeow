using Unity.Netcode;

using UnityEngine;



/// <summary>

/// Move o pickup em direção ao jogador mais próximo dentro do raio definido no SO (servidor).

/// </summary>

public class CienciaHoming : MonoBehaviour

{

    [SerializeField] private CienciaPickupConfig config;



    private Transform _target;

    private float _nextScanTime;

    private NetworkObject _networkObject;



    public void SetConfig(CienciaPickupConfig pickupConfig) => config = pickupConfig;



    private void Awake()

    {

        _networkObject = GetComponent<NetworkObject>();

    }



    private void FixedUpdate()

    {

        if (config == null) return;

        if (_networkObject != null)
        {
            if (NetworkManager.Singleton == null || !_networkObject.IsSpawned ||
                !NetworkManager.Singleton.IsServer)
                return;
        }



        if (Time.time >= _nextScanTime || _target == null)

        {

            _nextScanTime = Time.fixedTime + Mathf.Max(0.05f, config.playerScanInterval);

            _target = FindNearestPlayerTransform();

        }



        if (_target == null) return;



        Vector3 delta = _target.position - transform.position;

        float radiusSqr = config.homingRadius * config.homingRadius;

        if (delta.sqrMagnitude > radiusSqr)

        {

            _target = null;

            return;

        }



        Vector3 step = delta.normalized * (config.homingSpeed * Time.fixedDeltaTime);

        if (step.sqrMagnitude > delta.sqrMagnitude)

            transform.position = _target.position;

        else

            transform.position += step;

    }



    private Transform FindNearestPlayerTransform()

    {

        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer < 0) return null;



        Collider2D[] hits = Physics2D.OverlapCircleAll(

            transform.position,

            config.homingRadius,

            1 << playerLayer);



        Transform nearest = null;

        float bestDist = float.MaxValue;



        foreach (var hit in hits)

        {

            if (hit == null) continue;



            Transform candidate = hit.transform;

            var playerRoot = hit.GetComponentInParent<NetworkPlayerController>();

            if (playerRoot != null)

                candidate = playerRoot.transform;



            float dist = (candidate.position - transform.position).sqrMagnitude;

            if (dist < bestDist)

            {

                bestDist = dist;

                nearest = candidate;

            }

        }



        return nearest;

    }

}


