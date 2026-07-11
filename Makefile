.PHONY: format build test
FILE ?= file_name

format:
	@(dotnet format --verbosity normal)
build:
	@(dotnet build)
test:
	@(dotnet test)
