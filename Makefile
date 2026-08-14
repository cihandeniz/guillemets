.PHONY: format build test coverage init fix-owners
FILE ?= file_name
OWNER ?= $(shell whoami)
CLAUDE_USER ?= claudeuser
SETUP_SCRIPT_COMMIT := b02e0d4e11c04a80f774607d078c44481e332252
SETUP_SCRIPT_URL := https://raw.githubusercontent.com/cihandeniz/config-files/$(SETUP_SCRIPT_COMMIT)/claude/setup-claudedev-sandbox.sh
SETUP_SCRIPT_SHA256 := 672571d8412c65d8dbeb429346c25e5a4a751398814c33411cfb9f345e057e21
SETUP_SCRIPT := .tmp/scripts/setup-claudedev-sandbox.sh

format:
	@(dotnet format --verbosity normal)
build:
	@(dotnet build)
test:
	@(dotnet test)
coverage:
	@( \
		rm -rf .coverage && \
		dotnet test -c Release --collect:"XPlat Code Coverage" --settings ./test/runsettings.xml --results-directory .coverage && \
		dotnet reportgenerator -reports:.coverage/*/coverage.cobertura.xml -targetdir:.coverage/html && \
		open .coverage/html/index.html \
	)

$(SETUP_SCRIPT):
	@mkdir -p $(dir $(SETUP_SCRIPT))
	@curl -fsSL $(SETUP_SCRIPT_URL) -o $(SETUP_SCRIPT)
	@echo "$(SETUP_SCRIPT_SHA256)  $(SETUP_SCRIPT)" | sha256sum -c - || (rm -f $(SETUP_SCRIPT) && exit 1)
	@chmod +x $(SETUP_SCRIPT)

init: $(SETUP_SCRIPT)
	@sudo $(SETUP_SCRIPT) --owner $(OWNER) --claude-user $(CLAUDE_USER) --repo $(CURDIR)

fix-owners: init
