# HelloAI Functions sample

Run locally from the repository root:

```powershell
./src/FunctionApp.HelloAI/Start-Local.ps1
```

Requires Azure Functions Core Tools and Azurite (`npm install -g azurite`).
When `local.settings.json` uses `UseDevelopmentStorage=true`, the script starts
Azurite if needed and waits for its Blob, Queue, and Table endpoints before
starting Functions on port 7031. An emulator started by the script stops when
the script exits; an existing emulator is reused and left running.

For Visual Studio debugging, start Azurite with `azurite` before pressing F5.
