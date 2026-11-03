# Ideação do projeto de ciberproteção

Consolidação da ideação, no formato pedido: problema, solução inteligente e benefício
quantificável, com base no CyBOK (área de Ciclo de Vida de Desenvolvimento Seguro, dentro de
Segurança de Software e Plataforma).

## Problema / desafio

Um software comercial de neurofeedback é entregue como aplicativo instalado por um launcher e
executa inteiramente na máquina do cliente. Quando o software roda por completo no ambiente do
usuário, o fornecedor perde o controle sobre ele: quem possui a máquina pode inspecionar,
copiar e modificar o binário à vontade. Esse é o modelo de atacante conhecido como
**Man-At-The-End (MATE)**, diferente do atacante que está apenas na rede entre duas partes.

O aplicativo novo, construído em Avalonia/.NET, hoje não tem nenhum controle de segurança
("white canvas"). Desse cenário derivam duas dores com impacto direto no negócio:

1. **Pirataria**: uso do software sem licença válida.
2. **Roubo de propriedade intelectual**: recuperação do algoritmo proprietário de
   processamento de sinal a partir do binário.

## Solução inteligente

Em vez de tratar segurança como uma funcionalidade adicionada no fim, conduzir o software por
um **processo iterativo de endurecimento**, estruturado como Test-Driven Security com purple
teaming:

- O time azul constrói versões do app com controles de segurança crescentes, do mais simples
  ao mais avançado.
- O time vermelho tenta romper cada versão, seguindo árvores de ataque derivadas de uma
  modelagem de ameaças (STRIDE) sobre o cenário real.
- Cada ataque bem-sucedido é registrado como um teste que precisa ser corrigido e, uma vez
  corrigido, passa a servir de teste de regressão automatizado na esteira de CI.
- Cada controle e cada ataque são rastreados a uma área de conhecimento do CyBOK, a uma
  fraqueza (CWE) e a uma linha de defesa do projeto ASPIRE, de modo que as escolhas de
  segurança sejam fundamentadas e não arbitrárias.

A inteligência da proposta não está em um controle isolado, e sim no **processo replicável**:
o mesmo ciclo serve para qualquer software que rode em território hostil.

## Benefício quantificável

Em um cenário MATE não existe proteção absoluta: quem controla a máquina sempre pode, com
esforço suficiente, adulterar o binário. O objetivo viável, e a métrica do projeto, é
**elevar o custo do ataque acima do valor do ativo protegido**.

O benefício é medido de forma objetiva a cada iteração:

- **Cobertura da árvore de ataque**: número de folhas (caminhos de ataque) abertas por build.
  A meta é a progressão `n` folhas abertas no build 0 (baseline sem controle) para `m < n`
  nos builds seguintes.
- **Custo do ataque**: tempo/esforço que o red team gasta para romper cada controle, medido a
  cada rodada, na filosofia do ASPIRE de avaliar proteção por custo de ataque e não por
  binário seguro/inseguro.
- **Regressões evitadas**: número de ataques já corrigidos que a esteira barra automaticamente
  em builds futuros.

A entrega não é um software definitivamente blindado, o que seria inviável, mas a
**demonstração documentada de um processo de endurecimento**, acompanhada da medição de quanto
cada iteração encarece o ataque.
