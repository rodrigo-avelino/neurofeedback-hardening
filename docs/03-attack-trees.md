# Árvores de ataque

A árvore de ataque é o mapa do território, não o roteiro do jogo: ela lista todos os caminhos
conhecidos para atingir o objetivo do atacante. A raiz é o objetivo, as folhas são ataques
concretos e executáveis. Cada folha vira um teste da suíte de regressão (`security-tests/`).
Conectores OR significam que basta um caminho; AND significa que todos os filhos são
necessários.

O **estado** de cada folha é avaliado por build: `aberta` (ataque tem sucesso) ou `fechada`
(controle resiste). No build 0 (baseline, sem controles) quase tudo nasce aberto. A evolução do
projeto é essa coluna de estados ficando mais fechada a cada iteração.

## Árvore 1 - Usar o app sem licença válida (OR)

```
RAIZ: usar o app sem licença válida  (OR)
|
+-- G1 Adulterar o binário (tampering estático)  (OR)
|     F1.1 Editar o IL da validação (dnSpy/ILSpy) e salvar o assembly
|     F1.2 Patch de bytes: NOP no salto condicional
|     F1.3 Substituir o método de licença por um stub que sempre libera
|
+-- G2 Manipular em execução (tampering dinâmico)  (OR)
|     F2.1 Debugger: alterar o retorno da validação em runtime
|     F2.2 Hook/detour do método (ex. Harmony)
|     F2.3 Editar a memória do processo (flag "licenciado")
|
+-- G3 Forjar ou reusar a licença  (OR)
|     F3.1 Copiar uma licença válida de outra máquina (sem binding de hardware)
|     F3.2 Keygen: reverter o algoritmo de validação
|     F3.3 Editar o arquivo/registro de licença em claro
|     F3.4 Reset de trial (rollback de data, limpar marcador)
|
+-- G4 Contornar ativação online  (OR)  [ramo inativo ate adotar servidor]
|     F4.1 Emular o servidor de ativação
|     F4.2 Replay de uma resposta de ativação legítima
|     F4.3 Forçar o fallback permissivo offline
|
+-- G5 Derrotar a hardkey  (OR)  [ramo inativo ate adotar hardkey]
      F5.1 Emular o dongle em software
      F5.2 Clonar/dumpar o conteúdo do dongle
```

Observações que a árvore ensina:

- Enquanto o binário puder ser patchado (G1/G2), qualquer sofisticação em G3/G4/G5 é
  contornável por cima. A raiz real do problema é a integridade do binário.
- G4 e G5 nascem inativos: só entram em jogo quando o blue team adotar aquele controle. Estão
  desenhados para mostrar que os caminhos são conhecidos de antemão.

## Árvore 2 - Recuperar o algoritmo proprietário

```
RAIZ: recuperar o algoritmo de processamento de sinal  (OR)
|
+-- H1 Leitura direta  (OR)
|     H1.1 Descompilar o assembly (ILSpy/dnSpy) e ler o algoritmo
|     H1.2 Extrair parâmetros/pesos de configuração
|
+-- H2 Compreender o algoritmo localizado  (AND)
      H2.1 Localizar o módulo do algoritmo no binário
      H2.2 Compreender a lógica (nomes, fluxo, matemática)
```

Observações:

- H2 é um nó **AND**: para roubar de forma útil, o atacante precisa localizar E compreender. É
  exatamente onde a ofuscação atua, encarecendo H2.2.
- Teto honesto (MATE): nenhuma folha desta árvore fecha em definitivo enquanto o algoritmo
  estiver no cliente. Ofuscação e .NET Native AOT elevam o custo de H1 e H2, mas a mitigação que
  derrota o MATE é mover o núcleo para um servidor (o cliente deixa de conter o segredo). Isso
  vira recomendação arquitetural para a empresa.

## Diagrama

Há uma versão visual da Árvore 1 em `docs/assets/`. Para regenerar, usar o script de
diagramação do projeto (a ser adicionado).
