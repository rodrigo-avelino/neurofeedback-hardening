# Modelagem de ameaças

## 1. Cenário e modelo de atacante

O software executa 100% na máquina do cliente, instalado por um launcher. O modelo de atacante
é o **Man-At-The-End (MATE)**: o adversário controla o próprio ambiente de execução e pode
inspecionar, copiar e alterar o binário. Premissa que orienta todo o trabalho: contra quem
controla a máquina não há proteção absoluta; o objetivo é elevar o custo do ataque acima do
valor do ativo.

## 2. Diagrama de fluxo de dados (DFD)

```
   [Launcher] --(baixa/atualiza o app)--> (( canal de distribuição ))
        |                                          |
        | instala                                  | fronteira de confiança: supply chain
        v                                          v
   +-------------------------+        +----------------------------+
   |  App Avalonia/.NET       |<------>|  Arquivos locais            |
   |  (MVVM)                  | le/grava| (licença, config, sessão)  |
   +-----------+-------------+        +----------------------------+
        |              |
        | usa          | Bluetooth LE (fronteira de confiança)
        v              v
   [XNeuroSDK]     [BrainBit Flex 4 EEG]
```

Fronteiras de confiança (onde o controle do fornecedor termina):

- **F1 - Máquina do cliente**: território hostil. Quem instala tem admin e pode ler e adulterar
  tudo (o binário, os arquivos locais, a memória do processo).
- **F2 - Canal de distribuição do launcher**: de onde o app é baixado/atualizado. Se não houver
  verificação de assinatura, vira vetor de supply chain.
- **F3 - Bluetooth LE do EEG**: enlace entre app e sensor, passível de interceptação/injeção.

## 3. STRIDE por elemento

STRIDE é aplicado para derivar e justificar o escopo, não como atividade paralela. O resultado
mostra que o risco residual concentra em **Tampering** e **Information disclosure**, que viram
as duas histórias de atacante. Os demais vetores são de baixo valor neste produto (a leitura de
EEG de um sensor isolado, sem contexto clínico, diz pouco por si).

| Elemento | S | T | R | I | D | E | Observação |
|---|---|---|---|---|---|---|---|
| App (binário) | - | **Alto** | - | **Alto** | baixo | baixo | patch de licença; extração do algoritmo |
| Arquivos locais | - | **Médio** | - | médio | baixo | - | licença/sessão em claro |
| Launcher / canal | **Médio** | **Médio** | - | baixo | baixo | - | update malicioso (supply chain) |
| Bluetooth EEG | baixo | baixo | - | baixo | baixo | - | sinal de um sensor, baixo valor |

Legenda: S Spoofing, T Tampering, R Repudiation, I Information disclosure, D Denial of service,
E Elevation of privilege.

## 4. Adversários considerados

Ameaça sem adversário é paranoia sem foco. O trabalho considera dois perfis, que geram as duas
histórias:

- **Clínica pirata**: quer usar o software sem pagar. Motivação econômica, habilidade
  baixa/média. Gera a história de **pirataria**.
- **Concorrente**: quer o algoritmo proprietário de processamento de sinal. Habilidade alta.
  Gera a história de **roubo de propriedade intelectual**.

Fora de escopo (decisão registrada): o insider que fuça dados de paciente, porque o dado de EEG
de um sensor isolado tem baixo valor por si; e ataques de rede clássicos (MITM), porque o
software é local.

## 5. Posicionamento no CyBOK

- Processo: Secure Software Lifecycle (modelagem de ameaças, teste de segurança, melhoria
  iterativa).
- Modelo de atacante: MATE (literatura de software protection; projeto ASPIRE; OWASP
  MASVS-RESILIENCE).
- Método de modelagem: STRIDE, com as árvores de ataque em `03-attack-trees.md`.
