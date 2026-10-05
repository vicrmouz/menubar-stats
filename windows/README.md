# MenuBarStats para Windows

CPU e memória visíveis sobre a barra de tarefas, com painel de Sistema, Claude e Codex ao clicar. Mantém a composição compacta da versão macOS: ícones monocromáticos, números alinhados e barras de progresso finas.

## Executar

[Baixar o pacote portátil Windows x64 (beta)](https://github.com/vicrmouz/menubar-stats/releases/download/windows-v0.1.0-beta.1/MenuBarStats-windows-x64.zip).

Windows 10/11 x64. Extraia **a pasta inteira** do pacote portátil e abra `MenuBarStats.exe`. O pacote autocontido inclui o runtime .NET; não exige instalação ou administrador. Arraste a faixa ao longo da barra para reposicioná-la. Clique nela para abrir o painel; Escape ou clique fora fecha. O painel oferece Atualizar Claude, Iniciar com Windows e Sair.

O início automático vem desativado. A opção cria apenas `MenuBarStats.lnk` na pasta Startup do usuário e identifica sua autoria pela descrição do atalho. Um atalho preexistente de outra origem com esse nome é preservado; nesse caso, ativar a opção falha sem sobrescrevê-lo. Mantenha o pacote no mesmo lugar após ativar; ao mover a pasta, desative e reative a opção. Preferências ficam em `%LOCALAPPDATA%/MenuBarStats/settings.json`. Para desinstalar, desative Iniciar com Windows, saia do aplicativo e apague sua pasta e, se desejar, a pasta de preferências.

## Compilar

Com SDK .NET 8 no Windows:

```powershell
./windows/build.ps1
```

O script executa os testes e publica o pacote em `windows/artifacts/win-x64`. O workflow Windows também produz um artefato portátil em cada alteração do código Windows. Ele não cria releases automaticamente.

Para executar só os testes do núcleo, inclusive no macOS/Linux:

```sh
dotnet test windows/MenuBarStats.Tests -c Release
```

## Dados

- CPU, memória física, volume do Windows e tráfego de interfaces ativas são consultados a cada 2 segundos. CPU/rede precisam de duas amostras. A memória física do Windows tem semântica diferente da memória do Monitor de Atividade do macOS.
- Claude: credencial OAuth do Claude Code nativo em `%USERPROFILE%/.claude/.credentials.json`, ou na pasta indicada por `CLAUDE_CONFIG_DIR`; consulta a cada 5 minutos ou manualmente. Falta de token compatível, erro HTTP ou mudança do formato resulta em Sem dados. Não renova nem altera credenciais. O endpoint de uso da Anthropic é não documentado.
- Codex: registros locais em `%USERPROFILE%/.codex/sessions`, ou `CODEX_HOME/sessions`, consultados a cada minuto. Examina até 20 arquivos mais recentemente modificados, com no máximo 512 KB por arquivo. Valores refletem a última atividade encontrada; janelas expiradas aparecem com 0%. O formato local pode mudar.
- Instalações de Claude/Codex exclusivamente no WSL não são detectadas.
- Sem telemetria; tokens e conteúdo de sessões não são guardados em configurações/logs. Só a consulta de uso do Claude acessa a rede.

## Barra de tarefas e limites

A faixa é uma **sobreposição**, não uma extensão que reserva espaço no Explorer. Ela pode cobrir controles existentes: arraste para uma área livre. O posicionamento inicial tenta a região adjacente aos ícones de notificação; o Explorer pode mudar a estrutura dessa região entre versões.

Uma faixa acompanha a barra principal, inclusive mudanças de DPI/monitor e reinício do Explorer. A faixa se oculta quando a barra está fora da tela ou quando uma aplicação ocupa o monitor inteiro. Barras verticais do Windows 10 reduzem o espaço e podem diminuir a faixa. Não depende de ExplorerPatcher/Windhawk.

## Estado de validação

Em 2026-10-05, os 33 testes do núcleo passaram em Release no macOS, a compilação WPF terminou sem erros/avisos e o pacote Windows x64 autocontido foi publicado localmente. Isso **não valida** renderização WPF nem integração com Explorer. Antes de considerar a versão estável, executar a matriz abaixo em máquinas Windows 10 e 11:

- Temas claro/escuro e alto contraste; escala 100%, 150% e 200%.
- Barra inferior, superior e vertical quando disponíveis; ocultação automática.
- Tela cheia, dois monitores, troca de monitor principal e reinício do Explorer.
- Arrastar/restaurar posição; abrir/fechar painel e operar pelo teclado.
- Dados reais das ferramentas nativas e opção de início automático.

Fontes técnicas: [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/), [GetSystemTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes), [GlobalMemoryStatusEx](https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex).
