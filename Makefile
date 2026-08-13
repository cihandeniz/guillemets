.PHONY: format build test init fix-owners
FILE ?= file_name
OWNER ?= $(shell whoami)
CLAUDE_USER ?= claudeuser
SETUP_SCRIPT_URL := https://raw.githubusercontent.com/cihandeniz/config-files/main/claude/setup-claudedev-sandbox.sh
SETUP_SCRIPT := .tmp/scripts/setup-claudedev-sandbox.sh

format:
	@(dotnet format --verbosity normal)
build:
	@(dotnet build)
test:
	@(dotnet test)

$(SETUP_SCRIPT):
	@mkdir -p $(dir $(SETUP_SCRIPT))
	@curl -fsSL $(SETUP_SCRIPT_URL) -o $(SETUP_SCRIPT)
	@chmod +x $(SETUP_SCRIPT)

init: $(SETUP_SCRIPT)
	@sudo $(SETUP_SCRIPT) --owner $(OWNER) --claude-user $(CLAUDE_USER) --repo $(CURDIR)

fix-owners: init
