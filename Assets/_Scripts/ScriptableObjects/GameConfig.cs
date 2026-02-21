using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Delays (seconds)")]
    [Tooltip("Delay antes de carregar a cena de vitória")]
    public float victoryDelay = 2f;
    [Tooltip("Delay antes de carregar a cena de derrota")]
    public float defeatDelay = 2f;

    [Header("Scene Names")]
    [Tooltip("Nome da cena de vitória a ser carregada (build settings)")]
    public string victorySceneName;
    [Tooltip("Nome da cena de derrota a ser carregada (build settings)")]
    public string defeatSceneName;
}
