# Ambiente reprodutivel de desenvolvimento e analise (Linux).
#
# A regressao MATE roda na CI em runner Windows, a mesma plataforma do alvo e do red team
# (ver docs/06-plano-iteracoes.md). Este container cobre a higiene e a analise automatizada
# com as mesmas versoes da CI:
#   - SDK .NET 10 (o global.json da raiz escolhe a versao);
#   - ilspycmd e demais ferramentas .NET pelo manifesto dotnet-tools.json (dotnet tool restore);
#   - gitleaks na mesma versao fixada no job de segredos da esteira.
# A engenharia reversa interativa (dnSpyEx, x64dbg etc.) continua no Windows do red team.

FROM mcr.microsoft.com/dotnet/sdk:10.0

# gitleaks fixado por versao e conferido por SHA-256 (supply chain da propria esteira).
# Ao atualizar: trocar a versao e os dois hashes aqui e o GITLEAKS_VERSION em
# .github/workflows/security.yml.
ARG GITLEAKS_VERSION=8.30.1
ARG GITLEAKS_SHA256_X64=551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb
ARG GITLEAKS_SHA256_ARM64=e4a487ee7ccd7d3a7f7ec08657610aa3606637dab924210b3aee62570fb4b080

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl git \
    && rm -rf /var/lib/apt/lists/* \
    && case "$(dpkg --print-architecture)" in \
         amd64) arq=x64;   sha="$GITLEAKS_SHA256_X64" ;; \
         arm64) arq=arm64; sha="$GITLEAKS_SHA256_ARM64" ;; \
         *) echo "arquitetura sem gitleaks fixado: $(dpkg --print-architecture)" >&2; exit 1 ;; \
       esac \
    && curl -fsSL -o /tmp/gitleaks.tar.gz \
       "https://github.com/gitleaks/gitleaks/releases/download/v${GITLEAKS_VERSION}/gitleaks_${GITLEAKS_VERSION}_linux_${arq}.tar.gz" \
    && echo "${sha}  /tmp/gitleaks.tar.gz" | sha256sum -c - \
    && tar -xzf /tmp/gitleaks.tar.gz -C /usr/local/bin gitleaks \
    && rm /tmp/gitleaks.tar.gz

# Usuario sem privilegio. As ferramentas .NET sao locais ao repo (dotnet tool restore no
# postCreateCommand), entao nao dependem do HOME de quem as instalou.
ARG USERNAME=vscode
RUN useradd -m -s /bin/bash "$USERNAME"
USER $USERNAME
WORKDIR /workspace
