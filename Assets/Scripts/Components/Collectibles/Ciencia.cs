///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que representa a moeda do jogo (ciência) que pode ser coletada pelo jogador.
// ---------------------------------------------------------------- */

using UnityEngine;

public class Ciencia : MonoBehaviour
{
    private int _value;

    public void SetValue(int value)
    {
        _value = value;
    }

    public int GetValue()
    {
        return _value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GameEvents.InvokeCienciaCollected(_value);
            Destroy(gameObject);
        }
    }
}
