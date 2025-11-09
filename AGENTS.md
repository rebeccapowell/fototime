# Agent Instructions

- Use the locally installed .NET SDK at `~/.dotnet/dotnet` for all CLI operations. Before invoking the CLI, ensure the environment is prepared with:
  - `export DOTNET_ROOT=$HOME/.dotnet`
  - `export PATH=$PATH:$HOME/.dotnet`
- Always build the entire solution prior to running any tests using:
  - `~/.dotnet/dotnet build FotoTime.sln -v minimal -consoleloggerparameters:DisableConsoleColor`
- After a successful build, execute the test suite with:
  - `~/.dotnet/dotnet test --no-build -v minimal -consoleloggerparameters:DisableConsoleColor`
- Builds and tests must complete with **zero warnings**. Treat any warning as a failure that must be resolved.
- Respect the repository quality bar described in `README.md`: keep analyzers enabled, avoid suppressing diagnostics, and ensure new code is fully covered by tests.
- Integration tests require Docker; they are skipped automatically in this environment. Do not attempt to start Docker services here.
