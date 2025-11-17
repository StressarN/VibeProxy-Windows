# VibeProxy for Windows

> [!WARNING]
> **⚠️ HIGHLY EXPERIMENTAL BUT FUNCTIONAL**  
> This is an experimental Windows port that is actively under development. While it is functional and can be used, expect potential bugs and breaking changes. Use at your own risk and please report any issues you encounter!

<p align="center">
  <img src="icon.png" width="128" height="128" alt="VibeProxy Icon">
</p>

<p align="center">
<a href="https://github.com/StressarN/vibeproxy-windows"><img alt="By StressarN" src="https://img.shields.io/badge/By-StressarN-4b3baf" style="max-width: 100%;"></a>
<a href="https://github.com/StressarN/vibeproxy-windows/blob/main/LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-28a745" style="max-width: 100%;"></a>
<a href="https://github.com/StressarN/vibeproxy-windows"><img alt="Star this repo" src="https://img.shields.io/github/stars/StressarN/vibeproxy-windows.svg?style=social&amp;label=Star%20this%20repo&amp;maxAge=60" style="max-width: 100%;"></a></p>
</p>

**Stop paying twice for AI.** VibeProxy is a native Windows application that lets you use your existing Claude Code, ChatGPT, **Gemini**, and **Qwen** subscriptions with powerful AI coding tools like **[Factory Droids](https://app.factory.ai/r/FM8BJHFQ)** – no separate API keys required.

Built on [CLIProxyAPI](https://github.com/router-for-me/CLIProxyAPI), it handles OAuth authentication, token management, and API routing automatically. One click to authenticate, zero friction to code.

> [!IMPORTANT]
> **NEW: Gemini and Qwen Support! 🎉** VibeProxy now supports Google's Gemini AI and Qwen AI with full OAuth authentication. Connect your accounts and use Gemini and Qwen with your favorite AI coding tools!

> [!IMPORTANT]
> **NEW: Extended Thinking Support! 🧠** VibeProxy now supports Claude's extended thinking feature with dynamic budgets (4K, 10K, 32K tokens). Use model names like `claude-sonnet-4-5-20250929-thinking-10000` to enable extended thinking. See the [Factory Setup Guide](FACTORY_SETUP.md#step-3-configure-factory-cli) for details.

<p align="center">
<br>
  <a href="https://www.loom.com/share/5cf54acfc55049afba725ab443dd3777"><img src="vibeproxy-factory-video.webp" width="600" height="380" alt="VibeProxy Screenshot" border="0"></a>
</p>

> [!TIP]
> Check out our [Factory Setup Guide](FACTORY_SETUP.md) for step-by-step instructions on how to use VibeProxy with Factory Droids.


## Features

- 🎯 **Native Windows Experience** - Clean WPF interface designed for Windows
- 🚀 **One-Click Server Management** - Start/stop the proxy server with a single click
- 🔐 **OAuth Integration** - Authenticate with Codex, Claude Code, Gemini, and Qwen directly from the app
- 📊 **Real-Time Status** - Live connection status and automatic credential detection
- 🔄 **Auto-Updates** - Monitors auth files and updates UI in real-time
- 🎨 **Modern UI** - Clean interface with Windows 11 styling
- 💾 **Self-Contained** - Everything packaged together (server binary, config, static files)


## Installation

**⚠️ Requirements:** Windows 10 or later with .NET Desktop Runtime 8.0+

### Download Pre-built Release (Recommended)

1. Go to the [**Releases**](https://github.com/StressarN/vibeproxy-windows/releases) page
2. Download the latest Windows release package
3. Extract the ZIP file
4. Run `VibeProxy.Windows.exe`

### Build from Source

Want to build it yourself? See [**INSTALLATION.md**](INSTALLATION.md) for detailed build instructions.

## Usage

### First Launch

1. Launch `VibeProxy.Windows.exe`
2. The main window will open
3. The server will start automatically
4. Click "Connect" for Claude Code, Codex, Gemini, or Qwen to authenticate

### Authentication

When you click "Connect":
1. Your browser opens with the OAuth page
2. Complete the authentication in the browser
3. VibeProxy automatically detects your credentials
4. Status updates to show you're connected

### Server Management

- **Toggle Server**: Click Start/Stop to control the server
- **Status Display**: Shows whether the server is running or stopped
- **Launch at Startup**: Toggle to start VibeProxy automatically with Windows

## Requirements

- Windows 10 or later
- .NET Desktop Runtime 8.0 or later

## Development

### Project Structure

```
VibeProxy/
├── src/
│   └── VibeProxy.Windows/
│       ├── App.xaml               # Application entry point
│       ├── MainWindow.xaml        # Main UI window
│       ├── Services/              # Server and auth services
│       ├── ViewModels/            # MVVM view models
│       ├── Models/                # Data models
│       └── Resources/             # Icons and assets
├── tests/
│   └── VibeProxy.Windows.Tests/   # Unit tests
├── scripts/
│   └── build-windows.ps1          # Windows build script
├── VibeProxy.Windows.sln          # Visual Studio solution
└── Makefile                        # Build automation
```

### Key Components

- **MainWindow**: Main WPF window with server controls and authentication UI
- **CliProxyService**: Controls the cli-proxy-api server process
- **AuthStatusService**: Monitors `~/.cli-proxy-api/` for authentication files
- **ThinkingProxyServer**: Handles extended thinking mode transformations
- **SettingsViewModel**: MVVM pattern for managing application state
- **WPF UI**: Modern Windows interface with XAML styling and real-time updates

## Credits

This Windows port is maintained by **StressarN**.

**Special thanks to [Automaze](https://automaze.io) for creating the original VibeProxy project!** Their excellent work laid the foundation for this Windows implementation. The original macOS version and concept were brilliantly designed and executed by the Automaze team.

VibeProxy is built on top of [CLIProxyAPI](https://github.com/router-for-me/CLIProxyAPI), an excellent unified proxy server for AI services.

Special thanks to the CLIProxyAPI project for providing the core functionality that makes VibeProxy possible.

## License

MIT License - see LICENSE file for details

## Support

- **Report Issues**: [GitHub Issues](https://github.com/StressarN/vibeproxy-windows/issues)
- **Original Project**: [Automaze VibeProxy](https://github.com/automazeio/vibeproxy)

---

Windows port © 2025 StressarN. Original VibeProxy © 2025 [Automaze, Ltd.](https://automaze.io)
