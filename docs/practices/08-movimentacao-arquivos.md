# Movimentação de arquivos no Unity

## Regra crítica

**Sempre mover o par `arquivo` + `arquivo.meta` juntos.** O `.meta` guarda o GUID; sem ele, referências em prefabs, cenas e SOs quebram.

## Como mover

1. **Preferido:** arrastar no Unity Project window (atualiza referências e `.meta` automaticamente).
2. **Linha de comando:** mover pasta/arquivo **com** o `.meta` existente; nunca deixar o Unity gerar um `.meta` novo.
3. **Evitar:** `Set-Content -Encoding utf8` no PowerShell — isso grava **BOM UTF-8** e o Unity **não registra o GUID**, gerando “Missing Prefab” mesmo com o `guid:` correto no arquivo.

## Encoding correto dos `.meta`

Ao restaurar via script, use UTF-8 **sem BOM**:

```powershell
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText("Assets/.../PauseMenu.prefab.meta", $content, $utf8NoBom)
```

## Se os GUIDs já foram perdidos

Restaurar o conteúdo dos `.meta` a partir do Git (caminho antigo no `HEAD`):

```powershell
git show "HEAD:Assets/Prefabs/PauseMenu.prefab.meta" | Out-File "Assets/Prefabs/UI/PauseMenu.prefab.meta" -Encoding utf8NoBOM
```

*(No PowerShell 7+ use `-Encoding utf8NoBOM`; no Windows PowerShell 5, use `UTF8Encoding $false` como acima.)*

## Se o erro persistir após corrigir `.meta`

1. Feche o Unity.
2. Apague a pasta `Library/` do projeto (reimport completo), **ou**
3. No Editor: clique direito em `Assets/Prefabs` → **Reimport**.
4. Reabra o projeto e aguarde o import terminar.

## Após mover scripts

- Aguardar recompilação do Unity.
- Verificar `.asmdef` se existir (este projeto usa assembly padrão).
- Rodar Test Runner se houver testes.

## Estrutura alvo do projeto

Ver [docs/assets/STRUCTURE.md](../assets/STRUCTURE.md).

## Para agentes

Nunca sugerir “delete o .meta antigo”. Se reorganizar em lote, use o Editor ou restaure GUIDs via Git **sem BOM** antes de commitar.
