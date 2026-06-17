/// <summary>
/// Tipagem de dano para defesas específicas (ex.: Ranged Defense em inimigos).
/// Generic é o fallback quando nenhum tipo é informado.
/// </summary>
public enum DamageType
{
    Generic = 0,
    Melee = 1,
    Ranged = 2
}
