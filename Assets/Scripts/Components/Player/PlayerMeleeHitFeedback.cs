using UnityEngine;

/// <summary>
/// Juice local do melee Nix: afterimage + poeira no swing; spark/blink/shake no acerto.
/// A onda/trail do golpee continua em <see cref="MeleeAttackVisual"/>.
/// </summary>
[DisallowMultipleComponent]
public class PlayerMeleeHitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private float hitBlinkPulse = 1f;
    [SerializeField] private bool playSwingAfterimage = true;
    [SerializeField] private bool playSwingDust = true;
    [SerializeField] private Color afterimageColor = new Color(0.55f, 0.75f, 1f, 0.42f);
    [SerializeField] private float afterimageLifetime = 0.16f;
    [SerializeField] private float dustFeetOffsetY = -0.35f;

    private void Awake()
    {
        if (meleeCombat == null)
            meleeCombat = GetComponent<PlayerMeleeCombat>();
        if (playerAim == null)
            playerAim = GetComponent<PlayerAim>();
        if (bodyRenderer == null)
            bodyRenderer = ResolveBodyRenderer();
    }

    private void OnEnable()
    {
        if (meleeCombat == null)
            return;

        meleeCombat.OnMeleeAttackStarted += HandleMeleeAttackStarted;
        meleeCombat.OnMeleeHitsConfirmed += HandleMeleeHitsConfirmed;
    }

    private void OnDisable()
    {
        if (meleeCombat == null)
            return;

        meleeCombat.OnMeleeAttackStarted -= HandleMeleeAttackStarted;
        meleeCombat.OnMeleeHitsConfirmed -= HandleMeleeHitsConfirmed;
    }

    private void HandleMeleeAttackStarted()
    {
        if (bodyRenderer == null)
            bodyRenderer = ResolveBodyRenderer();

        Vector2 aim = Vector2.right;
        if (playerAim != null && playerAim.TryGetAimDirection(out Vector2 dir, out _))
            aim = dir;
        else if (bodyRenderer != null)
            aim = bodyRenderer.flipX ? Vector2.left : Vector2.right;

        if (playSwingAfterimage && bodyRenderer != null)
            MeleeSwingAfterimageVfx.Play(bodyRenderer, aim, afterimageColor, afterimageLifetime);

        if (playSwingDust)
        {
            Vector2 feet = transform.position;
            feet.y += dustFeetOffsetY;
            MeleeSwingDustVfx.Play(feet);
        }
    }

    private void HandleMeleeHitsConfirmed(MeleeHitResult result)
    {
        if (result.HitCount <= 0)
            return;

        float pulse = meleeCombat != null && meleeCombat.CombatStats != null
            ? Mathf.Max(meleeCombat.CombatStats.hitBlinkPulse, hitBlinkPulse)
            : hitBlinkPulse;

        // Melee flash um pouco mais evidente que o pulse padrão de dano.
        pulse = Mathf.Clamp01(pulse * 1.15f);

        PlayerCameraFeedback.ShakeOnMeleeHit(result.HitCount);

        for (int i = 0; i < result.HitPoints.Length; i++)
            MeleeHitBurstVfx.Play(result.HitPoints[i]);

        for (int i = 0; i < result.Targets.Length; i++)
        {
            GameObject target = result.Targets[i];
            if (target == null)
                continue;

            // Fase-3 boss: melee básico (1) não pisca — só hits marcantes via pipeline de dano.
            if (BossPhaseUtility.UsesCinematicBossPresentation(target))
                continue;

            if (target.TryGetComponent<SpriteBlink>(out var blink))
                blink.Pulse(pulse);
        }
    }

    private SpriteRenderer ResolveBodyRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.sprite == null)
                continue;

            // Ignora onda de hit / sombra no chão / ghosts.
            string n = sr.gameObject.name;
            if (n.Contains("MeleeHitWave") || n.Contains("Shadow") || n.Contains("Afterimage"))
                continue;

            if (sr.sortingOrder >= bestOrder)
            {
                bestOrder = sr.sortingOrder;
                best = sr;
            }
        }

        return best != null ? best : GetComponent<SpriteRenderer>();
    }
}
