import Foundation
import Observation

/// Estado observado pela UI. Sistema atualiza a cada 2 s, Codex a cada 1 min,
/// Claude a cada 5 min (é uma chamada de rede).
@MainActor
@Observable
final class StatsModel {
    private(set) var cpu: Double = 0
    private(set) var memoryUsed: UInt64 = 0
    let memoryTotal = ProcessInfo.processInfo.physicalMemory
    private(set) var diskFree: UInt64 = 0
    private(set) var diskTotal: UInt64 = 1
    private(set) var downloadRate: Double = 0   // bytes/s
    private(set) var uploadRate: Double = 0
    private(set) var claude: UsageLimits?
    private(set) var codex: UsageLimits?
    private(set) var claudeUpdatedAt: Date?

    private var lastTicks: [SystemReaders.CPUTicks] = []
    private var lastNet: (received: UInt64, sent: UInt64, at: Date)?
    private var tasks: [Task<Void, Never>] = []

    init() {
        tasks.append(repeating(every: .seconds(2)) { await $0.refreshSystem() })
        tasks.append(repeating(every: .seconds(60)) { await $0.refreshCodex() })
        tasks.append(repeating(every: .seconds(300)) { await $0.refreshClaude() })
    }

    func refreshClaude() async {
        if let limits = await ClaudeLimits.fetch() {
            claude = limits
            claudeUpdatedAt = .now
        }
    }

    private func refreshSystem() {
        if let ticks = SystemReaders.cpuTicks() {
            if let usage = SystemReaders.cpuUsage(from: lastTicks, to: ticks) { cpu = usage }
            lastTicks = ticks
        }
        if let used = SystemReaders.memoryUsed() { memoryUsed = used }
        if let disk = SystemReaders.disk() { (diskFree, diskTotal) = disk }
        if let net = SystemReaders.networkBytes() {
            let now = Date()
            if let last = lastNet, net.received >= last.received, net.sent >= last.sent {
                let elapsed = now.timeIntervalSince(last.at)
                downloadRate = Double(net.received - last.received) / elapsed
                uploadRate = Double(net.sent - last.sent) / elapsed
            }
            lastNet = (net.received, net.sent, now)
        }
    }

    private func refreshCodex() async {
        codex = await Task.detached(priority: .utility) { CodexLimits.read() }.value
    }

    private func repeating(every interval: Duration,
                           _ work: @escaping @MainActor (StatsModel) async -> Void) -> Task<Void, Never> {
        Task { [weak self] in
            while !Task.isCancelled {
                guard let self else { return }
                await work(self)
                try? await Task.sleep(for: interval)
            }
        }
    }
}
