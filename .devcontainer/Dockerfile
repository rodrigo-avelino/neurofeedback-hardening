# Ambiente reprodutivel usado pelo red team (parte automatizavel) e pela CI.
# A engenharia reversa interativa (dnSpy etc.) roda no Windows do red team;
# aqui ficam o toolchain .NET e as ferramentas de linha de comando para a
# regressao automatizada dos ataques.

FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG USERNAME=vscode
RUN useradd -m $USERNAME || true

# Ferramentas de analise/reversa em linha de comando:
#   ilspycmd  - descompilacao .NET (leitura de IL/C#)
#   (de4dot, dnlib via scripts) - manipulacao de assembly para os testes MATE
RUN dotnet tool install -g ilspycmd || true
ENV PATH="$PATH:/root/.dotnet/tools"

# gitleaks para checagem local de segredos (mesma usada na CI)
RUN apt-get update && apt-get install -y --no-install-recommends git curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace
USER $USERNAME
