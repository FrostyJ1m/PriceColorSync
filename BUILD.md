# Build on Windows

Requires Windows PowerShell, .NET Framework 4.x C# compiler, your own SPT 4.1.6 installation with BepInEx and Show Me The Money 3.0.2 installed on the client.

From this source directory:

```powershell
.\build.ps1 -ClientPath 'D:\Games\SPT'
```

The script reads dependency assemblies from that directory, compiles `build/SPT.Diagnostics.PriceColorSync.dll`, and compiles/runs two pure policy test executables (82 valuation/queue assertions and 31 menu-lifecycle assertions). It does not install the DLL, change the game installation, or launch the game/server. Compiler errors or failed tests stop the build.

Copy the output DLL manually into the player's `BepInEx/plugins` directory only after backing up an older version. Runtime testing remains necessary. Do not commit or redistribute proprietary game or third-party assemblies. The plugin C# files are unchanged from the local 1.0.8 source except the Forge-required plugin display name; the release build script adds an explicit ClientPath parameter for independent builds.