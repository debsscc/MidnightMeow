# Documentação de código

## Objetivo

Código legível por humanos e **interpretável por agentes** sem abrir cada método no Inspector.

## Cabeçalho do arquivo (obrigatório em scripts de gameplay)

```csharp
///* ----------------------------------------------------------------
// ATUALIZADO EM: DD-MM-AAAA
// DESCRIÇÃO: Resumo em uma ou duas frases do papel deste script.
// ---------------------------------------------------------------- */
```

Projetos legados podem usar `CRIADO EM` / `FEITO POR`; em alterações, atualize `ATUALIZADO EM` e a descrição se o papel mudou.

## Métodos públicos e protegidos

Use **XML documentation** (`/// <summary>`) em APIs expostas a outros sistemas ou ao editor (custom editors).

## Campos do Inspector

Todo `[SerializeField]` relevante para designers deve ter **`[Tooltip("...")]`** em português ou inglês consistente com o arquivo.

```csharp
[Tooltip("Taxa de tiro base antes dos bônus de upgrade.")]
[SerializeField] private float baseFireRate = 3f;
```

## Nomenclatura

Seguir [Convenções C# da Microsoft](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions):

- `PascalCase`: classes, métodos públicos, propriedades
- `_camelCase` ou `camelCase` para campos privados (manter consistência no arquivo)
- Interfaces: `I` + `PascalCase` (`IDamageable`)

## Para agentes de IA

Ao editar um script: adicione/atualize cabeçalho, tooltips em campos novos e XML docs em métodos públicos alterados. Não deixe “TODO” sem issue ou doc associada.
