# MenuBarStats para Windows — especificação

## Objetivo e requisitos aprovados

Adicionar uma versão para Windows 10 e Windows 11 neste repositório, mantendo a estética da versão macOS. CPU e memória ficam sempre visíveis sobre a barra de tarefas; um clique abre o painel de Sistema, Claude e Codex. A implementação Swift existente continua independente.

## Arquitetura proposta

Aplicativo C# com WPF e .NET 8, em `windows/`, sem navegador embutido. WPF permite desenhar a faixa e o painel com controle de layout e escala. A distribuição inicial será um pacote portátil autocontido para Windows x64; ARM64 fica fora da primeira versão.

Separar interface, estado/agendamento, leitores de sistema, leitores de limites e integração Win32 da barra. Os leitores retornam snapshots; falhas de uma fonte não interrompem as outras. Atualizações da interface ocorrem na thread de UI; leituras de arquivos e HTTP não bloqueiam essa thread. Só uma instância pode executar por usuário.

## Aparência e interação

A faixa exibe ícone de CPU, percentual, ícone de memória e percentual, em uma linha, com números de largura fixa, aproximadamente 13 unidades lógicas de tamanho e espaçamento semelhante ao SwiftUI atual. Ícones vetoriais próprios substituem SF Symbols. Texto e fundo acompanham o tema claro/escuro do Windows.

O painel tem 300 unidades lógicas de largura, padding de 16 e separadores entre Sistema, Claude, Codex e ações. Usa cantos arredondados, fundo discreto e barras finas: cor de destaque até 60%, laranja acima de 60%, vermelho acima de 85%. Preservar os rótulos portugueses e as ações Atualizar Claude e Sair. Escape ou clique fora fecha o painel; Enter/Espaço na faixa também o abre. Nomes acessíveis identificam métricas e ações.

## Integração com a barra de tarefas

Usar uma janela sem bordas sobre a barra, sem entrada própria na lista de tarefas. É uma sobreposição: não reserva espaço no Explorer nem desloca os ícones existentes. Evitar injeção de DLLs e alterações no Explorer. A faixa permanece sobre a barra quando visível, mas não sobre aplicações em tela cheia.

O posicionamento inicial tenta uma área livre adjacente à região do relógio na barra principal. Se não houver espaço, permitir arrastar a faixa ao longo da barra e salvar o deslocamento localmente. Essa limitação deve constar no README: não existe garantia de espaço livre em toda configuração.

Monitorar posição, tamanho e visibilidade da barra; recalcular após mudança de resolução, DPI, tema, monitor ou reinício do Explorer. Na ocultação automática, esconder a faixa junto com a barra e restaurá-la quando a barra reaparecer. O painel abre junto à faixa e é limitado à área visível do monitor. Na primeira versão, mostrar uma única faixa na barra principal; monitores secundários devem funcionar ao mover a barra principal, sem duplicar a faixa.

## Fontes de dados

- Sistema a cada 2 segundos: CPU por diferença dos tempos globais via `GetSystemTimes`; memória física usada/total via `GlobalMemoryStatusEx`; capacidade e espaço livre do volume do Windows via `DriveInfo`; tráfego por diferença dos contadores das interfaces ativas não loopback. A primeira amostra de CPU/rede estabelece a referência. Reset de contador ou troca de interface não produz taxas negativas.
- Claude a cada 5 minutos e no botão: adaptar o leitor já existente para ler credenciais locais do Claude Code no Windows, considerando `CLAUDE_CONFIG_DIR` e, na ausência dele, a pasta `.claude` do usuário. O arquivo `.credentials.json` é um formato de integração sujeito a validação na implementação. Ler apenas o token necessário; não copiar, registrar, renovar ou modificar credenciais. Usar o endpoint e cabeçalho atuais do projeto, timeout de 15 segundos e nenhuma chamada concorrente. Ausência de OAuth compatível, arquivo inválido ou falha HTTP mostra Sem dados. O endpoint é não documentado e sua disponibilidade não é garantida.
- Codex a cada minuto: ler JSONL em `CODEX_HOME/sessions`, ou `.codex/sessions` no perfil do usuário. Reutilizar a semântica de `rate_limits.primary/secondary.used_percent` e `resets_at`. Examinar finais de arquivos com leitura limitada, tolerando linhas incompletas enquanto o CLI escreve. Janela expirada exibe 0%; ausência de evento exibe Sem dados. Indicar que os valores refletem a última atividade registrada.

Somente instalações nativas das ferramentas no Windows são detectadas inicialmente. Instalações e credenciais dentro de WSL ficam fora deste escopo. Não enviar telemetria nem conteúdo de sessões. A única comunicação externa do aplicativo é a consulta de uso do Claude à Anthropic.

## Distribuição e configuração

Fornecer script PowerShell de build/publicação e instruções para executar o pacote portátil. Adicionar opção explícita Iniciar com Windows, desativada por padrão, por atalho na pasta Startup do usuário; desativar remove apenas o atalho criado pelo app. Salvar preferências de posição e início automático em uma pasta própria de `%LOCALAPPDATA%`, sem guardar tokens.

Adicionar workflow de CI com runner Windows para compilar, executar testes e produzir o pacote. Não publicar release nem instalar o aplicativo no computador do usuário nesta etapa.

## Validação e critérios de aceite

Testar leitores de limites com fixtures sintéticas: campos ausentes, JSON inválido, linhas parciais, percentuais, datas, janela expirada e seleção de evento recente. Testar cálculo de deltas de CPU/rede e limites de posicionamento com entradas determinísticas. Não usar credenciais reais em testes ou logs.

CI deve compilar e passar os testes. Verificação visual e de integração exige Windows: testar Windows 10 e 11, temas claro/escuro, escala 100/150/200%, auto-hide, tela cheia, dois monitores e reinício do Explorer. Comparar hierarquia e espaçamento com `Sources/MenuBarStats/MenuBarStatsApp.swift`.

O ambiente atual é macOS: compilação em CI não comprova comportamento visual e posicionamento. A entrega deve declarar quais verificações Windows foram efetivamente executadas e quais ainda exigem máquina Windows.

## Alternativas consideradas

- DeskBand no Windows 10 e sobreposição no Windows 11: descartado para evitar duas implementações de integração e comportamento divergente.
- Apenas ícone na área de notificação: não atende ao requisito de percentuais sempre visíveis.
- Electron/Tauri: não traz vantagem para este painel pequeno e exige outra camada de UI; WPF mantém controle direto de janelas Win32.

## Referências

- Interface atual: `Sources/MenuBarStats/MenuBarStatsApp.swift`.
- Fontes atuais: `Sources/MenuBarStats/UsageLimits.swift` e contexto em `~/brain/50-Projetos/menubar-stats/INDEX.md`.
- WPF: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- Discussão sobre ausência de DeskBands no Windows 11: https://github.com/microsoft/WindowsAppSDK/discussions/2320
