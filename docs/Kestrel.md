## Kestrel — Brief Explanation

**Kestrel** is the **cross-platform web server** built into ASP.NET Core. It's the component that actually listens for HTTP requests on the network and serves responses.

### Key Points

| Aspect | Detail |
|--------|--------| 
| **What it is** | The embedded web server that replaces IIS, Apache, or Nginx for most ASP.NET Core apps |
| **Who makes it** | Microsoft (named after the kestrel bird of prey) |
| **Platform** | Cross-platform — works on Windows, Linux, and macOS |
| **What it does** | Listens on HTTP ports, handles connections, routes requests to your application pipeline |

### How It Relates to Your App

In `Test-IA.WebApp`, Kestrel is started automatically when you call `app.Run()` in `Program.cs`. It reads the binding URLs from:

1. `ASPNETCORE_URLS` environment variable
2. Command-line `--urls` argument
3. `launchSettings.json` (only during `dotnet run`)

When we set `ASPNETCORE_URLS=<PROTOCOL>://<SERVER>:<PORT>`, we're telling Kestrel to **listen on port `<PORT>` on the `<SERVER>` hostname**.

### Kestrel vs IIS

| | Kestrel | IIS |
|---|---------|-----|
| **Included with .NET** | Yes | No (Windows feature) |
| **Standalone** | Can run alone | Requires IIS hosting |
| **Reverse proxy** | Usually behind IIS/Nginx | Can serve directly |
| **Your app** | Runs standalone (self-contained) | Would run under IIS |

Your self-contained deployment package runs Kestrel **standalone** — no IIS needed.
