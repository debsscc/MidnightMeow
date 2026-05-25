using UnityEngine;

public enum TelegraphShapeType
{
    Circle = 0,
    Rectangle = 1
}

/// <summary>Como o preenchimento visual progride.</summary>
public enum TelegraphFillMode
{
    /// <summary>Círculo: do centro para fora. Retângulo: ao longo do eixo local Y (comprimento).</summary>
    ExpandFromOrigin = 0,
    /// <summary>Retângulo: preenche do fim (alvo) em direção à origem — útil para faixas de projétil.</summary>
    AlongLengthTowardOrigin = 1
}

public enum EnemyTelegraphResolution
{
    /// <summary>Dano na zona ao fim do telegraph. Opcional <c>effectPrefab</c> na zona (ex.: pedras caindo).</summary>
    AreaDamage = 0,
    /// <summary>Visual do inimigo até a zona; dano aplicado só na zona ao chegar (não dispara para cima).</summary>
    ProjectileToZone = 1
}
