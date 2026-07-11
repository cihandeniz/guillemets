.PHONY: format build test init
FILE ?= file_name
OWNER ?= $(shell whoami)
CLAUDE_USER ?= claudeuser

format:
	@(dotnet format --verbosity normal)
build:
	@(dotnet build)
test:
	@(dotnet test)
init:
	@sudo scripts/setup-claudedev-sandbox.sh --owner $(OWNER) --claude-user $(CLAUDE_USER) --repo $(CURDIR)
