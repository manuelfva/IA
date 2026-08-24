# Test-IA.WebApp Deployment Guide

## Overview

This is a self-contained deployment of Test-IA.WebApp, an ASP.NET Core Razor Pages application for Active Directory user and group information retrieval.

## Prerequisites

- **Windows machine** joined to an Active Directory domain
- **.NET 10.0 Runtime** is NOT required (bundled with this package)
- Network access to at least one Domain Controller
- User account running the app must have LDAP read access to the domain and must be authorized to update users and groups managed by the app
- Must define SPNs for the user account running the app (PROTOCOL = HTTP and/or HTTPS):
   ```powershell
   setspn -S <PROTOCOL>/<SERVER> <DOMAIN>\<USER-SAMACCOUNTNAME>, for example: setspn -S http://covadonga-srv:5000
   setspn -S <PROTOCOL>/<SERVER-FQDN> <DOMAIN>\<USER-SAMACCOUNTNAME>, for example: setspn -S https://covadonga-srv.asturmalaga.com:5000
   ```
- Add service URLs to the policy `Computer Configuration → Policies → Administrative Templates → Windows Components → Internet Explorer → Internet Control Panel → Security Page → Site to Zone Assignment List`:
  ```
  <PROTOCOL>://<SERVER>:<PORT>      1, for example: http://covadonga-srv:5000                   1 
  <PROTOCOL>://<SERVER-FQDN>:<PORT> 1, for example: http://covadonga-srv.asturmalaga.com:5000   1 
  ```
- The server where the app is running must have an domain `inbound rule` to allow TCP and UDP `<PORT>` communication  

## Deployment Steps

1. **Extract** this package to your desired installation folder:
   ```powershell
   Expand-Archive Test-IA.WebApp-deploy-*.zip -DestinationPath "C:\WebApps\Test-IA"
   ```

2. **Configure** (optional): Edit `appsettings.json` to override:
   - `Authorization.RequiredGroup`: AD group required for access
   - `Logging.Sinks`: Additional logging targets
   - `Logging:LogLevel`: Per-namespace log levels

3. **Run** the application:

   To make the ASP.NET Core web app listen on `<PROTOCOL>://<SERVER>:<PORT>`, we need to understand how ASP.NET Core determines binding URLs.

   ***¿How ASP.NET Core Reads URLs?***   

   By default, ASP.NET Core reads binding URLs from (in priority order):  
   1. Command-line arguments (--urls)
   2. Environment variable (ASPNETCORE_URLS)
   3. launchSettings.json (only when running via dotnet run)
   4. appsettings.json — NOT read by default unless explicitly wired up

   Since this is a published/self-contained deployment, launchSettings.json is not used.  
   And by default, ASP.NET Core does not read URL bindings from appsettings.json.

   There are three viable approaches:

   - Environment Variable (Recommended for Production)
     Set the ASPNETCORE_URLS environment variable on the target machine:
     
     ```powershell
     # Set as system environment variable
     [Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "<PROTOCOL>://<SERVER>:<PORT>", "Machine")
     ```  

     Then restart the app. No code or appsettings.json change needed.

   - Command-Line Argument
     Run the executable with the --urls flag: 
     ```powershell
     .\Test-IA.WebApp.exe --urls "<PROTOCOL>://<SERVER>:<PORT>"
     ```  
     No `appsettings.json` change needed.

   - Update `appsettings.json + Code Change`: 
     
     Add a Urls section to `appsettings.json` and modify the `Program.cs` to explicitly read it from configuration.

     This requires a code change in the `WebApp project`.

1. **Access** the application in a browser:
   ```
   <PROTOCOL>://<SERVER>:<PORT>
   ```

## Running as a Windows Service (Production)

To run as a background service:

1. Install `nssm` (Non-Sucking Service Manager):
   ```powershell
   choco install nssm
   ```

2. Register the service:
   ```powershell
   nssm install Test-IA.WebApp "C:\WebApps\Test-IA\Test-IA.WebApp.exe"
   nssm set Test-IA.WebApp Directory "C:\WebApps\Test-IA"
   nssm start Test-IA.WebApp
   ```

## Configuration

The application reads configuration from:

1. `appsettings.json` (bundled)
2. `appsettings.{Environment}.json` (e.g., `appsettings.Production.json`)
3. Environment variables:
   - `ASPNETCORE_ENVIRONMENT` is the only environment variable explicitly read in code (`app.Environment.IsDevelopment()`).
   - `ASPNETCORE_URLS` is consumed by `Kestrel` automatically — no application code reads it.
4. Command-line arguments

### Key Configuration Keys

| Key | Default | Description |
|-----|---------|-------------|
| `Authorization:RequiredGroup` | `Domain Admins` | AD group members can access the app |
| `Logging:LogLevel:Default` | `Information` | Default log level |
| `Logging:LogLevel:TestIA` | `Debug` | Log level for Test-IA namespace |

## Troubleshooting

### Application fails to start

- Ensure the machine is joined to the Active Directory domain:
  ```powershell
  Get-CimInstance Win32_ComputerSystem | Select-Object PartOfDomain, Domain
  ```
- Check Windows event logs for detailed error messages.

### Authentication fails

- Ensure your user account is a member of the required AD group.
- Verify network access to a Domain Controller:
  ```powershell
  nslookup $(Get-CimInstance Win32_ComputerSystem).Domain
  ```

### LDAPS / Secure LDAP

If your domain requires LDAPS, ensure the Domain Controller has a valid certificate and the machine trusts the issuing CA.  
The application will automatically use LDAPS when configured.

## Package Contents

| File/Folder | Description |
|-------------|-------------|
| `Test-IA.WebApp.exe` | Self-contained executable (includes .NET runtime) |
| `appsettings.json` | Default configuration |
| `appsettings.Development.json` | Development overrides |
| `logs/` | Application log directory |
| `wwwroot/` | Static web assets (CSS, JS) |
| `Pages/` | Razor Pages (embedded) |

## Support

For issues, check the application logs in the `logs/` directory or contact the development team.
