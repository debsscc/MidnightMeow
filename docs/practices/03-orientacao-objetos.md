# Orientação a objetos (POO)

## Objetivo

Modelar o domínio do jogo com **classes coesas**, reutilização via herança/polimorfismo onde faz sentido, e contratos claros.

## Aplicação no MidnightMeow

### Abstrações e interfaces

- `IDamageable` — contrato para entidades que recebem dano.
- `Ability` (base) — habilidades do jogador com implementações concretas (`Ability_ProjectileReflect`, etc.).

### Composição vs herança

**Preferir composição** em `MonoBehaviour`: o `Player` agrega `PlayerMovement`, `PlayerShooting`, `HealthComponent`, em vez de uma mega-classe `Player` com 2000 linhas.

Herança é adequada quando há **variação real de algoritmo** (tipos de inimigo, tipos de projétil, habilidades).

### Polimorfismo

Inimigos compartilham comportamentos via componentes (`EnemyMovement`, `EnemyAttack_Ranged`) e stats via `EnemyStats` SO — o “tipo” é dados + combinação de componentes, não só subclass.

## Convenções C#

- Um tipo público por arquivo, nome do arquivo = nome da classe.
- Campos privados: `_camelCase` ou `camelCase` conforme padrão já usado no arquivo tocado.
- Propriedades públicas somente leitura quando expõem estado derivado de SO.

## Para agentes de IA

Ao estender inimigos ou habilidades: pergunte se é **novo dado** (SO), **novo componente** (SRP) ou **nova subclasse** (polimorfismo). Na dúvida, SO + componente novo.
