// ----------------------------------------------------------------------------
// FEITO POR: DEBS CARVALHO
// ÚLTIMA ATUALIZAÇÃO: 2026-06-18
// DESCRIÇÃO: Configuração de apresentação de créditos.
// ----------------------------------------------------------------------------

using System;
using UnityEngine;

// Comportamento ao fim da rolagem. Passado por chamada para não acoplar menu/pause/fim de jogo.

[Serializable]
public struct CreditsPresentationConfig
{
    public CreditsEndBehavior EndBehavior;
    public float HoldAtEndSeconds;
    public float FadeOutSeconds;
    public float EndScrollPadding;

    // Padrão menu/pause: para no final, espera, escurece e fecha.
    public static CreditsPresentationConfig DefaultMenu => DefaultPause;

    // Igual ao menu — usado ao abrir do pause (solo ou MP).
    public static CreditsPresentationConfig DefaultPause => new()
    {
        EndBehavior = CreditsEndBehavior.HoldThenFadeClose,
        HoldAtEndSeconds = 3f,
        FadeOutSeconds = 1f,
        EndScrollPadding = 72f,
    };

    // Para no final; usuário fecha manualmente (ex.: vitória, reel longo).
    public static CreditsPresentationConfig ManualClose => new()
    {
        EndBehavior = CreditsEndBehavior.HoldUntilManualClose,
        HoldAtEndSeconds = 0f,
        FadeOutSeconds = 0f,
        EndScrollPadding = 72f,
    };
}

public enum CreditsEndBehavior
{
    HoldThenFadeClose,
    HoldUntilManualClose,
}
