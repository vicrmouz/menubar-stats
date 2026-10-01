import SwiftUI

@main
struct MenuBarStatsApp: App {
    private let model = StatsModel()

    var body: some Scene {
        MenuBarExtra {
            StatsPanel(model: model)
        } label: {
            // Rótulo curto: com o notch, itens largos são escondidos pelo macOS.
            MenuBarLabel(cpu: model.cpu, memory: Double(model.memoryUsed) / Double(model.memoryTotal))
        }
        .menuBarExtraStyle(.window)
    }
}

/// O MenuBarExtra descarta SF Symbols misturados com texto no rótulo, então o rótulo
/// é desenhado como uma única imagem template (segue o tema claro/escuro da barra).
struct MenuBarLabel: View {
    let cpu: Double
    let memory: Double

    var body: some View {
        Image(nsImage: rendered)
    }

    private var rendered: NSImage {
        let content = HStack(spacing: 3) {
            Image(systemName: "cpu")
            Text(Format.percent(cpu))
            Image(systemName: "memorychip").padding(.leading, 5)
            Text(Format.percent(memory))
        }
        .font(.system(size: 13, weight: .medium).monospacedDigit())
        .imageScale(.medium)

        let renderer = ImageRenderer(content: content)
        renderer.scale = NSScreen.main?.backingScaleFactor ?? 2
        let image = renderer.nsImage ?? NSImage()
        image.isTemplate = true
        return image
    }
}

struct StatsPanel: View {
    let model: StatsModel

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Section("Sistema") {
                Meter(title: "CPU", value: model.cpu, detail: Format.percent(model.cpu))
                Meter(title: "Memória", value: Double(model.memoryUsed) / Double(model.memoryTotal),
                      detail: "\(Format.gigabytes(model.memoryUsed)) de \(Format.gigabytes(model.memoryTotal))")
                Meter(title: "Disco", value: 1 - Double(model.diskFree) / Double(model.diskTotal),
                      detail: "\(Format.gigabytes(model.diskFree)) livres")
                HStack {
                    Text("Rede").font(.subheadline.weight(.medium))
                    Spacer()
                    Text("↓ \(Format.rate(model.downloadRate))   ↑ \(Format.rate(model.uploadRate))")
                        .monospacedDigit().foregroundStyle(.secondary)
                }
            }
            Divider()
            LimitsSection(title: "Claude", limits: model.claude)
            Divider()
            LimitsSection(title: "Codex", limits: model.codex)
            Divider()
            HStack {
                Button("Atualizar Claude") { Task { await model.refreshClaude() } }
                Spacer()
                Button("Sair") { NSApplication.shared.terminate(nil) }
                    .keyboardShortcut("q")
            }
            .buttonStyle(.borderless)
        }
        .padding(16)
        .frame(width: 300)
    }
}

struct Section<Content: View>: View {
    let title: String
    @ViewBuilder let content: Content
    init(_ title: String, @ViewBuilder content: () -> Content) {
        self.title = title
        self.content = content()
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(title).font(.headline)
            content
        }
    }
}

struct LimitsSection: View {
    let title: String
    let limits: UsageLimits?

    var body: some View {
        Section(title) {
            if let limits {
                Meter(title: "Sessão (5h)", value: limits.session.percent / 100,
                      detail: detail(limits.session))
                Meter(title: "Semana", value: limits.weekly.percent / 100,
                      detail: detail(limits.weekly))
            } else {
                Text("Sem dados").foregroundStyle(.secondary)
            }
        }
    }

    private func detail(_ window: LimitWindow) -> String {
        let pct = "\(Int(window.percent.rounded()))%"
        guard let reset = window.resetsAt else { return pct }
        return "\(pct) · reinicia \(reset.formatted(.relative(presentation: .named)))"
    }
}

struct Meter: View {
    let title: String
    let value: Double
    let detail: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text(title).font(.subheadline.weight(.medium))
                Spacer()
                Text(detail).font(.caption).monospacedDigit().foregroundStyle(.secondary)
            }
            ProgressView(value: min(max(value, 0), 1))
                .tint(value > 0.85 ? .red : value > 0.6 ? .orange : .accentColor)
                .animation(.easeOut(duration: 0.3), value: value)
        }
    }
}

enum Format {
    static func percent(_ fraction: Double) -> String {
        "\(Int((fraction * 100).rounded()))%"
    }

    static func gigabytes(_ bytes: UInt64) -> String {
        String(format: "%.1f GB", Double(bytes) / 1_000_000_000)
    }

    static func rate(_ bytesPerSecond: Double) -> String {
        switch bytesPerSecond {
        case ..<1_000: String(format: "%.0f B/s", bytesPerSecond)
        case ..<1_000_000: String(format: "%.0f KB/s", bytesPerSecond / 1_000)
        default: String(format: "%.1f MB/s", bytesPerSecond / 1_000_000)
        }
    }
}
