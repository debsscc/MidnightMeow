/// <summary>
/// Como a cena deve ser carregada (local ou via Netcode no host).
/// </summary>
public enum SceneLoadKind
{
    SinglePlayer = 0,
    /// <summary>Host chama NetworkManager.SceneManager.LoadScene.</summary>
    NetcodeHost = 1
}
