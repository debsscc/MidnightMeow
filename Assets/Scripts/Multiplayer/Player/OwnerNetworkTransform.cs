/// <summary>
/// OwnerNetworkTransform.cs
/// NetworkTransform com autoridade do dono (owner-authoritative).
/// Substitui o NetworkTransform padrão (server-authoritative) no prefab do jogador.
///
/// POR QUÊ É NECESSÁRIO:
///   No NGO 2.x, NetworkTransform por padrão é server-authoritative:
///   o servidor define a posição e corrige clientes que divergem (rubber band).
///   Para um jogo onde cada cliente controla seu próprio personagem, precisamos
///   que o DONO (owner) tenha autoridade sobre a posição — o servidor apenas replica.
///
/// CONFIGURAÇÃO NO EDITOR:
///   No prefab do jogador, REMOVA o componente NetworkTransform padrão (se existir)
///   e adicione este componente OwnerNetworkTransform no lugar.
///   Todos os outros campos (Position, Rotation, Scale sync) funcionam igual.
///
/// SRP: apenas define a autoridade do NetworkTransform.
/// </summary>

using Unity.Netcode.Components;

public class OwnerNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Retorna false para indicar que a autoridade é do dono (owner),
    /// não do servidor. Chamado internamente pelo NGO a cada tick de sincronização.
    /// </summary>
    protected override bool OnIsServerAuthoritative() => false;
}
