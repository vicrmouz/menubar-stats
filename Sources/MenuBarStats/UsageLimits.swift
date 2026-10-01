import Foundation

struct LimitWindow: Sendable {
    var percent: Double      // 0...100
    var resetsAt: Date?
}

struct UsageLimits: Sendable {
    var session: LimitWindow     // janela de 5 horas
    var weekly: LimitWindow      // janela de 7 dias
}

/// Limites do Claude: usa o token OAuth que o próprio Claude Code guarda no Keychain
/// e consulta o mesmo endpoint de uso que o `/usage` do Claude Code.
enum ClaudeLimits {
    static func fetch() async -> UsageLimits? {
        guard let token = accessToken() else { return nil }
        var request = URLRequest(url: URL(string: "https://api.anthropic.com/api/oauth/usage")!)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("oauth-2025-04-20", forHTTPHeaderField: "anthropic-beta")
        request.timeoutInterval = 15
        guard let (data, response) = try? await URLSession.shared.data(for: request),
              (response as? HTTPURLResponse)?.statusCode == 200,
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return nil }

        func window(_ key: String) -> LimitWindow? {
            guard let w = json[key] as? [String: Any], let pct = w["utilization"] as? Double else { return nil }
            return LimitWindow(percent: pct, resetsAt: (w["resets_at"] as? String).flatMap(parseISODate))
        }
        guard let session = window("five_hour"), let weekly = window("seven_day") else { return nil }
        return UsageLimits(session: session, weekly: weekly)
    }

    /// Lê via `/usr/bin/security`, que já tem acesso ao item e evita o pedido de senha do Keychain.
    private static func accessToken() -> String? {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/security")
        process.arguments = ["find-generic-password", "-s", "Claude Code-credentials", "-w"]
        let pipe = Pipe()
        process.standardOutput = pipe
        process.standardError = FileHandle.nullDevice
        guard (try? process.run()) != nil else { return nil }
        let data = pipe.fileHandleForReading.readDataToEndOfFile()
        process.waitUntilExit()
        guard process.terminationStatus == 0,
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let oauth = json["claudeAiOauth"] as? [String: Any] else { return nil }
        return oauth["accessToken"] as? String
    }

    private static func parseISODate(_ string: String) -> Date? {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter.date(from: string) ?? ISO8601DateFormatter().date(from: string)
    }
}

/// Limites do Codex: o CLI grava `rate_limits` nos eventos `token_count` das sessões
/// em ~/.codex/sessions/AAAA/MM/DD/*.jsonl. Lemos o evento mais recente.
enum CodexLimits {
    static func read() -> UsageLimits? {
        let root = FileManager.default.homeDirectoryForCurrentUser.appending(path: ".codex/sessions")
        guard let enumerator = FileManager.default.enumerator(
            at: root, includingPropertiesForKeys: [.contentModificationDateKey]) else { return nil }
        let files = enumerator.compactMap { $0 as? URL }
            .filter { $0.pathExtension == "jsonl" }
            .map { ($0, (try? $0.resourceValues(forKeys: [.contentModificationDateKey]).contentModificationDate) ?? .distantPast) }
            .sorted { $0.1 > $1.1 }
            .prefix(5)

        for (file, _) in files {
            if let limits = lastLimits(in: file) { return limits }
        }
        return nil
    }

    private static func lastLimits(in file: URL) -> UsageLimits? {
        guard let handle = try? FileHandle(forReadingFrom: file) else { return nil }
        defer { try? handle.close() }
        // Só o final do arquivo: as sessões podem ter dezenas de MB.
        let size = (try? handle.seekToEnd()) ?? 0
        try? handle.seek(toOffset: size > 512_000 ? size - 512_000 : 0)
        guard let data = try? handle.readToEnd(), let text = String(data: data, encoding: .utf8) else { return nil }

        for line in text.split(separator: "\n").reversed() where line.contains("\"rate_limits\"") {
            guard let json = try? JSONSerialization.jsonObject(with: Data(line.utf8)) as? [String: Any],
                  let payload = json["payload"] as? [String: Any],
                  let limits = payload["rate_limits"] as? [String: Any],
                  let primary = window(limits["primary"]),
                  let secondary = window(limits["secondary"]) else { continue }
            return UsageLimits(session: primary, weekly: secondary)
        }
        return nil
    }

    private static func window(_ value: Any?) -> LimitWindow? {
        guard let w = value as? [String: Any], let pct = w["used_percent"] as? Double else { return nil }
        let reset = (w["resets_at"] as? Double).map { Date(timeIntervalSince1970: $0) }
        // Evento antigo cuja janela já reiniciou: o uso real voltou a zero.
        if let reset, reset < .now { return LimitWindow(percent: 0, resetsAt: nil) }
        return LimitWindow(percent: pct, resetsAt: reset)
    }
}
