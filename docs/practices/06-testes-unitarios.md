# Testes unitários (NUnit)

## Objetivo

Garantir que alterações **não quebrem** lógica pura nem contratos entre componentes. Testes rodam fora do play mode quando possível.

## Stack

- **Unity Test Framework** + **NUnit**
- Pasta de testes: `Assets/Tests/` (Edit Mode e Play Mode conforme necessidade)
- Assembly runtime: `Assets/Scripts/MidnightMeow.asmdef` (código do jogo referenciável por testes)
- Assembly Edit Mode: `MidnightMeow.Tests.EditMode.asmdef` com `includePlatforms: ["Editor"]` (obrigatório para builds IL2CPP)

## O que testar

| Categoria | Exemplos |
|-----------|----------|
| Lógica pura | `UpgradeDefinition.GetBonusForLevel`, cálculos de dano, timers |
| Serviços | `ServiceLocator` (registro/resolução), stores de lobby |
| Integração leve | Inicialização `PlayerInitializer` com mocks de SO |
| Rede | Preferir testes de métodos estáticos/helpers; gameplay Netcode em Play Mode tests |

## Quando são obrigatórios

- Nova funcionalidade com regra de negócio
- Correção de bug (regressão)
- Refatoração que mexe em API pública

## Execução

No Unity: **Window → General → Test Runner** (Edit Mode / Play Mode).

Em CI (futuro): `Unity -runTests -batchmode -projectPath ... -testResults ...`

## Padrão de nome

- Assembly: `MidnightMeow.Tests.EditMode`
- Classe: `{ClassUnderTest}Tests`
- Método: `{Method}_{Scenario}_{Expected}`

```csharp
[Test]
public void GetBonusForLevel_Level2_ReturnsExpectedMultiplier()
{
    // Arrange / Act / Assert
}
```

## Para agentes de IA

Ao fechar uma tarefa de código: rodar Test Runner ou documentar por que o teste não se aplica (ex.: só mudança visual). Não mergear lógica crítica sem teste de regressão.
