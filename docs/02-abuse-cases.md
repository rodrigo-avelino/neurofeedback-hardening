# Casos de abuso (histórias do atacante)

Casos de abuso descrevem o comportamento do sistema sob um ator malicioso, o oposto das
histórias de usuário. Cada ator mau recebe um objetivo e os caminhos que ele desejaria
percorrer. As mitigações viram requisitos de segurança e alimentam a árvore de ataque e a
suíte de testes de regressão.

## AC-1 - Pirataria

> **Como** clínica pirata, **quero** executar o software sem uma licença válida, **para** não
> pagar pelo produto.

- **Ator**: clínica pirata (motivação econômica, habilidade baixa/média).
- **Ativo alvo**: a verificação de licença.
- **Gatilho**: o app roda localmente, então todo o mecanismo de licença está na mão do atacante.
- **Caminhos desejados** (detalhados na árvore G1 a G5 de `03-attack-trees.md`):
  - adulterar o binário para pular a verificação (patch no IL, NOP no salto condicional);
  - manipular a verificação em tempo de execução (debugger, hook, edição de memória);
  - forjar ou reusar o artefato de licença (copiar de outra máquina, keygen, editar em claro);
  - contornar ativação online, se adotada (emular servidor, replay);
  - derrotar a hardkey, se adotada (emular/clonar o dongle).
- **Requisito de segurança derivado**: o app deve recusar execução sem licença válida, e a
  verificação deve resistir a adulteração estática e dinâmica dentro do custo-alvo.
- **Critério de aceite (teste)**: rodar o app com cada técnica acima deve resultar em recusa de
  execução. Ver `security-tests/`.

## AC-2 - Roubo do algoritmo proprietário

> **Como** concorrente, **quero** recuperar o pipeline proprietário de processamento de sinal
> a partir do binário, **para** replicá-lo no meu produto.

- **Ator**: concorrente (habilidade alta).
- **Ativo alvo**: o algoritmo de processamento de sinal (no repositório, substituído por um stub).
- **Gatilho**: binário .NET descompila com facilidade (IL legível).
- **Caminhos desejados**:
  - descompilar o assembly e ler o algoritmo diretamente;
  - localizar o módulo do algoritmo e compreendê-lo (nó AND: precisa localizar E compreender);
  - extrair pesos/parâmetros de configuração.
- **Requisito de segurança derivado**: elevar o custo de localizar e compreender o algoritmo; e,
  como recomendação arquitetural, avaliar mover o núcleo sensível para fora do cliente.
- **Nota honesta (MATE)**: a única mitigação que de fato derrota o MATE aqui é **não enviar o
  segredo** (mover o pipeline para um servidor). Mantendo tudo local, ofuscação e .NET Native
  AOT são endurecimento parcial, não solução. Isso é um achado do trabalho, não uma falha.
- **Critério de aceite (teste)**: medir o tempo que o red team leva para extrair o algoritmo
  stub a cada iteração; a meta é a curva de custo subir.

## Casos de uso indevido (misuse) fora de escopo

Registrados para rastreabilidade, mas fora do escopo acordado: vazamento de dados de paciente
(baixo valor neste cenário) e ataques de rede (software local).
