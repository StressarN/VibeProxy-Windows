.PHONY: build app install clean run help windows-build windows-release windows-help

PWSH ?= pwsh
WINDOWS_BUILD_SCRIPT := scripts/build-windows.ps1

ifeq ($(OS),Windows_NT)
PWSH_CHECK = @where $(PWSH) >nul 2>&1 || (echo ❌ PowerShell '$(PWSH)' not found. Install pwsh to continue. && exit /b 1)
else
PWSH_CHECK = @command -v $(PWSH) >/dev/null 2>&1 || { echo "❌ PowerShell '$(PWSH)' not found. Install pwsh to continue."; exit 1; }
endif

help: ## Show this help message
	@echo "VibeProxy - macOS Menu Bar App"
	@echo ""
	@echo "Available targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

build: ## Build the Swift executable (debug)
	@echo "🔨 Building Swift executable..."
	@cd src && swift build
	@echo "✅ Build complete: src/.build/debug/CLIProxyMenuBar"

release: ## Build the Swift executable (release)
	@echo "🔨 Building Swift executable (release)..."
	@./build.sh
	@echo "✅ Build complete: src/.build/release/CLIProxyMenuBar"

app: ## Create the .app bundle
	@echo "📦 Creating .app bundle..."
	@./create-app-bundle.sh
	@echo "✅ App bundle created: VibeProxy.app"

install: app ## Build and install to /Applications
	@echo "📲 Installing to /Applications..."
	@rm -rf "/Applications/VibeProxy.app"
	@cp -r "VibeProxy.app" /Applications/
	@echo "✅ Installed to /Applications/VibeProxy.app"

run: app ## Build and run the app
	@echo "🚀 Launching app..."
	@open "VibeProxy.app"

clean: ## Clean build artifacts
	@echo "🧹 Cleaning..."
	@rm -rf src/.build
	@rm -rf "VibeProxy.app"
	@rm -rf src/Sources/Resources/cli-proxy-api
	@rm -rf src/Sources/Resources/config.yaml
	@rm -rf src/Sources/Resources/static
	@echo "✅ Clean complete"

test: ## Run a quick test build
	@echo "🧪 Testing build..."
	@cd src && swift build
	@echo "✅ Test build successful"

info: ## Show project information
	@echo "Project: VibeProxy - macOS Menu Bar App"
	@echo "Language: Swift 5.9+"
	@echo "Platform: macOS 13.0+"
	@echo ""
	@echo "Files:"
	@find src/Sources -name "*.swift" -exec wc -l {} + | tail -1 | awk '{print "  Swift code: " $$1 " lines"}'
	@echo "  Documentation: 4 files"
	@echo ""
	@echo "Structure:"
	@tree -L 3 -I ".build" || echo "  (install 'tree' for better output)"

open: ## Open app bundle to inspect contents
	@if [ -d "VibeProxy.app" ]; then \
		open "VibeProxy.app"; \
	else \
		echo "❌ App bundle not found. Run 'make app' first."; \
	fi

edit-config: ## Edit the bundled config.yaml
	@if [ -d "VibeProxy.app" ]; then \
		open -e "VibeProxy.app/Contents/Resources/config.yaml"; \
	else \
		echo "❌ App bundle not found. Run 'make app' first."; \
	fi

windows-help: ## Show prerequisites for Windows builds
	@echo "🪟 Windows build targets require PowerShell 7+ (pwsh) and .NET SDK on a Windows host."
	@echo "Run 'make windows-build' or 'make windows-release' from Windows to produce artifacts in windows/out/."

windows-build: ## Build Windows artifacts in Debug configuration via PowerShell script
	$(PWSH_CHECK)
	@$(PWSH) -NoProfile -ExecutionPolicy Bypass -File $(WINDOWS_BUILD_SCRIPT) -Configuration Debug

windows-release: ## Build Windows artifacts in Release configuration via PowerShell script
	$(PWSH_CHECK)
	@$(PWSH) -NoProfile -ExecutionPolicy Bypass -File $(WINDOWS_BUILD_SCRIPT) -Configuration Release

# Shortcuts
all: app ## Same as 'app'
