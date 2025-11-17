.PHONY: build release clean help info

PWSH ?= pwsh
WINDOWS_BUILD_SCRIPT := scripts/build-windows.ps1

ifeq ($(OS),Windows_NT)
PWSH_CHECK = @where $(PWSH) >nul 2>&1 || (echo ❌ PowerShell '$(PWSH)' not found. Install pwsh to continue. && exit /b 1)
else
PWSH_CHECK = @command -v $(PWSH) >/dev/null 2>&1 || { echo "❌ PowerShell '$(PWSH)' not found. Install pwsh to continue."; exit 1; }
endif

help: ## Show this help message
	@echo "VibeProxy - Windows Application"
	@echo ""
	@echo "Available targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

build: ## Build Windows artifacts in Debug configuration
	$(PWSH_CHECK)
	@$(PWSH) -NoProfile -ExecutionPolicy Bypass -File $(WINDOWS_BUILD_SCRIPT) -Configuration Debug

release: ## Build Windows artifacts in Release configuration
	$(PWSH_CHECK)
	@$(PWSH) -NoProfile -ExecutionPolicy Bypass -File $(WINDOWS_BUILD_SCRIPT) -Configuration Release

clean: ## Clean build artifacts
	@echo "🧹 Cleaning Windows build artifacts..."
	@if [ -d "out" ]; then rm -rf out; fi
	@if [ -d "src/VibeProxy.Windows/bin" ]; then rm -rf src/VibeProxy.Windows/bin; fi
	@if [ -d "src/VibeProxy.Windows/obj" ]; then rm -rf src/VibeProxy.Windows/obj; fi
	@if [ -d "tests/VibeProxy.Windows.Tests/bin" ]; then rm -rf tests/VibeProxy.Windows.Tests/bin; fi
	@if [ -d "tests/VibeProxy.Windows.Tests/obj" ]; then rm -rf tests/VibeProxy.Windows.Tests/obj; fi
	@echo "✅ Clean complete"

info: ## Show project information
	@echo "Project: VibeProxy - Windows Application"
	@echo "Language: C# / .NET"
	@echo "Platform: Windows 10+"
	@echo ""
	@echo "Build Requirements:"
	@echo "  - PowerShell 7+ (pwsh)"
	@echo "  - .NET SDK 8.0 or later"
	@echo ""
	@echo "Structure:"
	@tree -L 3 -I "bin|obj|out" || echo "  (install 'tree' for better output)"

# Shortcuts
all: release ## Same as 'release'
