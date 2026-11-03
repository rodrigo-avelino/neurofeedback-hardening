# app/ - aplicacao figurada (mock)

Este diretorio hospeda uma **aplicacao figurada** com a mesma arquitetura do produto real
(Avalonia/.NET/MVVM, launcher, verificacao de licenca, leitura de SDK, armazenamento local de
sessao), mas com o nucleo de processamento de sinal substituido por um **stub** sem valor
comercial (por exemplo, um filtro generico ou gerador de ruido com a mesma assinatura de
funcao do modulo real).

Por que mock:

- preserva as superficies de ataque que importam para o trabalho (licenca, integridade do
  binario, arquivos locais), identicas as do produto real;
- nao expoe a propriedade intelectual da empresa (o pipeline de DSP);
- quando o processo estiver validado, basta trocar o stub pelo modulo real no ambiente
  fechado da empresa, e os controles ja vem testados.

Cuidado registrado: o stub precisa ter complexidade parecida com a do real no ponto que a
defesa protege. Para testar ocultacao de algoritmo, o stub deve ser um algoritmo de verdade,
nao um `return input`, senao a conclusao nao transfere.

## Estado

- [ ] Esqueleto Avalonia/.NET
- [ ] Verificacao de licenca (build 0: ingenua, de proposito)
- [ ] Stub do nucleo de processamento
- [ ] Launcher
