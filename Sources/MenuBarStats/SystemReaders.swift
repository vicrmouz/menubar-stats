import Darwin
import Foundation

/// Leituras de CPU, RAM, disco e rede via APIs do kernel. Funções puras + estado anterior
/// guardado por quem chama, para calcular taxas por diferença entre amostras.
enum SystemReaders {
    struct CPUTicks: Sendable { var busy: UInt64; var total: UInt64 }

    static func cpuTicks() -> [CPUTicks]? {
        var cpuCount: natural_t = 0
        var info: processor_info_array_t?
        var infoCount: mach_msg_type_number_t = 0
        guard host_processor_info(mach_host_self(), PROCESSOR_CPU_LOAD_INFO,
                                  &cpuCount, &info, &infoCount) == KERN_SUCCESS,
              let info else { return nil }
        defer {
            vm_deallocate(mach_task_self_, vm_address_t(bitPattern: info),
                          vm_size_t(infoCount) * vm_size_t(MemoryLayout<integer_t>.stride))
        }
        return (0..<Int(cpuCount)).map { cpu in
            let base = cpu * Int(CPU_STATE_MAX)
            func tick(_ state: Int32) -> UInt64 { UInt64(UInt32(bitPattern: info[base + Int(state)])) }
            let busy = tick(CPU_STATE_USER) + tick(CPU_STATE_SYSTEM) + tick(CPU_STATE_NICE)
            return CPUTicks(busy: busy, total: busy + tick(CPU_STATE_IDLE))
        }
    }

    static func cpuUsage(from old: [CPUTicks], to new: [CPUTicks]) -> Double? {
        guard old.count == new.count else { return nil }
        var busy: UInt64 = 0, total: UInt64 = 0
        for (a, b) in zip(old, new) {
            busy &+= b.busy &- a.busy
            total &+= b.total &- a.total
        }
        return total == 0 ? 0 : Double(busy) / Double(total)
    }

    /// "Memória usada" no critério do Monitor de Atividade: apps + residente + comprimida.
    static func memoryUsed() -> UInt64? {
        var stats = vm_statistics64()
        var count = mach_msg_type_number_t(MemoryLayout<vm_statistics64>.stride / MemoryLayout<integer_t>.stride)
        let result = withUnsafeMutablePointer(to: &stats) {
            $0.withMemoryRebound(to: integer_t.self, capacity: Int(count)) {
                host_statistics64(mach_host_self(), HOST_VM_INFO64, $0, &count)
            }
        }
        guard result == KERN_SUCCESS else { return nil }
        let pageSize = UInt64(sysconf(_SC_PAGESIZE))
        let appPages = UInt64(stats.internal_page_count) - UInt64(stats.purgeable_count)
        return (appPages + UInt64(stats.wire_count) + UInt64(stats.compressor_page_count)) * pageSize
    }

    /// Espaço do volume de inicialização, no mesmo critério do Finder (inclui espaço purgável).
    static func disk() -> (free: UInt64, total: UInt64)? {
        let keys: Set<URLResourceKey> = [.volumeAvailableCapacityForImportantUsageKey, .volumeTotalCapacityKey]
        guard let values = try? URL(fileURLWithPath: "/").resourceValues(forKeys: keys),
              let free = values.volumeAvailableCapacityForImportantUsage,
              let total = values.volumeTotalCapacity else { return nil }
        return (UInt64(free), UInt64(total))
    }

    /// Bytes totais recebidos/enviados em todas as interfaces físicas (exceto loopback).
    static func networkBytes() -> (received: UInt64, sent: UInt64)? {
        var head: UnsafeMutablePointer<ifaddrs>?
        guard getifaddrs(&head) == 0, let head else { return nil }
        defer { freeifaddrs(head) }
        var received: UInt64 = 0, sent: UInt64 = 0
        for ptr in sequence(first: head, next: { $0.pointee.ifa_next }) {
            let ifa = ptr.pointee
            guard let addr = ifa.ifa_addr, addr.pointee.sa_family == UInt8(AF_LINK),
                  ifa.ifa_flags & UInt32(IFF_LOOPBACK) == 0,
                  let data = ifa.ifa_data?.assumingMemoryBound(to: if_data.self) else { continue }
            let name = String(cString: ifa.ifa_name)
            // utun/awdl/llw/bridge duplicam tráfego de VPN, AirDrop e afins
            guard name.hasPrefix("en") else { continue }
            received += UInt64(data.pointee.ifi_ibytes)
            sent += UInt64(data.pointee.ifi_obytes)
        }
        return (received, sent)
    }
}
