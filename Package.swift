// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "MenuBarStats",
    platforms: [.macOS(.v14)],
    targets: [
        .executableTarget(name: "MenuBarStats", path: "Sources/MenuBarStats")
    ]
)
