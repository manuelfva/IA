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

## SSL Configuration via Active Directory Certificate Services (AD CS)

This section describes how to obtain and install an SSL/TLS certificate from your Active Directory Certificate Services (AD CS) infrastructure, and configure the WebApp to serve HTTPS traffic.

### Prerequisites

- **Active Directory Certificate Services (AD CS)** role installed and configured on a server in your domain.
- A **Web Server (Server Authentication)** certificate template available and published in AD CS.
- The target machine must have a **Domain-joined service account** with permission to request certificates from the template.
- The certificate **Common Name (CN)** must match the **FQDN** that external clients will use to reach the WebApp (e.g., `covadonga-srv.asturmalaga.com`).
- The certificate must include the FQDN as a **Subject Alternative Name (SAN)** entry.

### Step 1 — Verify the Certificate Template

We will use **InternalWebServer** template.  
This template was created and is already published in AD CS:

```powershell
# Install module for ADCS management
Install-Module -Name PSPKI -Scope CurrentUser
Import-Module PSPKI  

# List all available certificate templates
Get-CertificateTemplate | Where-Object { $_.Name -like "*InternalWebServer*" }

# Verify the template details
(Get-CertificateTemplate | Where-Object { $_.Name -like "*InternalWebServer*" }).Policies
```

Confirm the template has:
- **Key Length**: 2048 bits minimum
- **Cryptographic Provider**: RSA-Schannel
- **SAN Extension**: Enabled
- **Enrollment Permissions**: Your service account has **Read** and **Enroll**

### Step 2 — Request the Certificate on the Target Machine

Run the following on the **target server** (the machine that will host the WebApp) under the service account context:

```powershell
# Create a certificate request INF file (.TrimStart() removes the leading newline from the here-string)
@"
[NewRequest]
Subject = "CN=covadonga-srv.asturmalaga.com"
KeyLength = 2048
KeyAlgorithm = RSA
HashAlgorithm = SHA256
Exportable = TRUE
MachineKeySet = TRUE
KeyUsage = 0xA0
ProviderName = "Microsoft RSA SChannel Cryptographic Provider"
ProviderType = 12
RequestType = PKCS10

[Extensions]
2.5.29.17 = "{text}"
_continue_ = "DNS=covadonga-srv.asturmalaga.com&"
_continue_ = "DNS=covadonga-srv"
"@.TrimStart() | Set-Content -Path certreq.inf -Encoding ASCII

# Verify the file starts correctly (first line MUST be [NewRequest], no empty lines before it)
Get-Content certreq.inf -TotalCount 3

# Request the certificate from AD CS
certreq -new certreq.inf certreq.cer

# Submit the request (replace CA-SERVER and TemplateName as appropriate)
certreq -submit -config "TORROX-SRV\asturmalaga-TORROX-SRV-CA" -attrib "certificateTemplate:InternalWebServer" certreq.cer certreq.crt

# Approve the request via CA Console (if auto-approval is not configured), then:
certreq -accept certreq.crt
```

Alternatively, if the machine has **IIS Manager** installed:

1. Open **IIS Manager** → Server Certificates → **Create Certificate Request**.
2. Fill in Common Name (FQDN), State, Country.
3. Bit length: 2048, Cryptographic provider: Microsoft RSA SChannel Cryptographic Provider.
4. Submit to your AD CS, then **Complete Certificate Request** and select the **Personal** certificate store.

### Step 3 — Verify the Certificate Installation

The certificate is already installed by `certreq -accept`. Verify it is in the **Local Machine → Personal** store:

```powershell
# List certificates matching the FQDN
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*covadonga-srv*" }
```

Confirm the output shows your certificate with an **Expiration Date** in the future and **Key Container** populated.

### Step 4 — Bind the Certificate to the HTTPS URL

ASP.NET Core Kestrel reads the binding URLs from `ASPNETCORE_URLS`.

To enable HTTPS, set the environment variable and bind the certificate:

```powershell
# Set the HTTPS URL
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "https://+:5001", "Machine")

# Bind the certificate to the port using appsettings.json
"Kestrel": {
    "Certificates": {
      "Default": {
        "Subject": "covadonga-srv.asturmalaga.com",
        "Store": "My",
        "Location": "LocalMachine",
        "AllowInvalid": false
      }
    }
  }
```

### Step 5 — Create SPNs for HTTPS

Kerberos/Windows Authentication requires:

- The service class must be HTTP, not https.
  Even if the traffic is encrypted via TLS, the SPN for web authentication is always registered as HTTP/...   
  It's a fixed Windows/Kerberos convention, independent of whether you use HTTP or HTTPS underneath.  

- It must not include the port.
  The client (browser, HttpClient, etc.) requests the Kerberos ticket for HTTP/covadonga-srv.asturmalaga.com without :5001,   
  regardless of the actual connection port.

```powershell
setspn -S HTTP/covadonga-srv.asturmalaga.com asturmal\administrador
setspn -S HTTP/covadonga-srv asturmal\administrador
setspn -Q HTTP/covadonga-srv.asturmalaga.com
```

After applying it, on the remote machine, clear the Kerberos ticket cache before retrying: 
```
klist purge
```

### Step 6 — Update Firewall Rules

Open the HTTPS port in the Windows Firewall:

```powershell
# Create an inbound rule for HTTPS
New-NetFirewallRule -DisplayName "Test-IA.WebApp HTTPS" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 5001 `
    -Action Allow
```

### Step 7 — Restart the Application

```powershell
# If running as a Windows Service
Restart-Service Test-IA.WebApp

# If running as a standalone process
Stop-Process -Name Test-IA.WebApp -Force
.\Test-IA.WebApp.exe
```

### Step 8 — Verify HTTPS Connectivity

```powershell
# Test the HTTPS endpoint
Invoke-WebRequest -Uri "https://covadonga-srv.asturmalaga.com:5001" -UseBasicParsing -UseDefaultCredentials

# Verify the certificate chain
Invoke-WebRequest -Uri "https://covadonga-srv.asturmalaga.com:5001" -UseBasicParsing -UseDefaultCredentials | Select-Object -ExpandProperty Content
```

### Step 9 — Add URLs to Intranet Zone

We have to add in `Computer Configuration → Policies → Administrative Templates → Windows Components → Internet Explorer → Internet Control Panel → Security Page → Site to Zone Assignment List`  
the following two values: 
```
https://covadonga-srv:5001                      1
https://covadonga-srv.asturmalaga.com:5001      1
```

### Troubleshooting SSL/AD CS

| Problem | Resolution |
|---------|------------|
| Certificate not trusted by clients | Ensure the AD CS Root CA certificate is installed in the **Trusted Root Certification Authorities** store on every client machine, or distribute it via Group Policy. |
| SPN registration conflict | Verify uniqueness with `setspn -Q HTTP/covadonga-srv.asturmalaga.com`. Resolve duplicates before re-registering. |
| Kestrel fails to start on HTTPS | Check that the certificate's private key is accessible by the service account. The key must not be marked as export-only without the private key. |
| Browser shows certificate warning | Verify the certificate CN/SAN matches the URL exactly. Check expiration dates and revocation status (CRL/OCSP). |

### Security Considerations

- **Certificate Rotation**: Plan certificate renewal before expiration. AD CS templates support automatic renewal if configured.
- **Key Protection**: Ensure the certificate private key is stored securely and is not exportable in production.
- **Chain of Trust**: All clients must trust the AD CS Root CA. Distribute via Group Policy Object (GPO) for enterprise-wide trust.
- **Minimum Key Length**: Do not use keys shorter than 2048 bits.
- **Revocation**: Monitor CRL/OCSP availability. Certificates marked as revoked will cause authentication failures.

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
