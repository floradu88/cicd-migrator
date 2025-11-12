# Running FileDownloadApi

## Prerequisites

- .NET Framework 4.8 installed
- IIS Express or IIS installed
- Visual Studio 2017+ (recommended) or MSBuild

## Method 1: Run in Visual Studio (Recommended)

1. **Open the solution:**
   - Open `DatabaseExtractor.sln` in Visual Studio

2. **Set FileDownloadApi as startup project:**
   - Right-click `FileDownloadApi` project → Set as Startup Project

3. **Build the solution:**
   - Press `Ctrl+Shift+B` or Build → Build Solution
   - Ensure `DatabaseExtractor` project builds first (it's a dependency)

4. **Run the API:**
   - Press `F5` or click the Run button
   - IIS Express will start automatically
   - Default URL: `http://localhost:8080` (check Output window for actual port)

5. **Verify it's running:**
   - Open browser to: `http://localhost:8080/api/files`
   - Should return JSON with file list (may be empty if no files)

## Method 2: Run with IIS Express (Command Line)

1. **Build the project:**
   ```powershell
   msbuild FileDownloadApi\FileDownloadApi.csproj /t:Build /p:Configuration=Debug
   ```

2. **Start IIS Express:**
   ```powershell
   cd FileDownloadApi
   "C:\Program Files\IIS Express\iisexpress.exe" /path:. /port:8080
   ```

   Or if IIS Express is in PATH:
   ```powershell
   iisexpress /path:FileDownloadApi /port:8080
   ```

## Method 3: Deploy to IIS

1. **Build in Release mode:**
   ```powershell
   msbuild FileDownloadApi\FileDownloadApi.csproj /t:Build /p:Configuration=Release
   ```

2. **Publish to IIS:**
   - In Visual Studio: Right-click project → Publish
   - Or copy `bin` folder contents to IIS application directory

3. **Configure IIS:**
   - Create Application Pool targeting .NET Framework 4.8
   - Create Application pointing to published folder
   - Set appropriate permissions on files directory

## Testing the API

Once running, test the endpoints:

### 1. List Files (should work even if empty)
```powershell
Invoke-RestMethod -Uri "http://localhost:8080/api/files"
```

### 2. Test Connection Endpoint
```powershell
$body = @{
    connectionString = "Server=(local);Database=master;Integrated Security=True;"
    timeoutSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/files/test-connection" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### 3. List Databases Endpoint
```powershell
$body = @{
    connectionString = "Server=(local);Integrated Security=True;"
    timeoutSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/files/list-databases" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

## Troubleshooting

### Issue: "Could not load file or assembly"
- **Solution:** Ensure all NuGet packages are restored:
  ```powershell
  nuget restore FileDownloadApi\packages.config
  ```

### Issue: "500 Internal Server Error"
- **Solution:** Check Windows Event Viewer for detailed errors
- Verify `DatabaseExtractor.dll` is in `bin` folder
- Check Web.config for correct framework version (should be 4.8)

### Issue: "404 Not Found" for API routes
- **Solution:** Verify Web API routing is configured in `WebApiConfig.cs`
- Check that `Global.asax.cs` calls `WebApiConfig.Register`

### Issue: Port already in use
- **Solution:** Change port in project properties or IIS Express configuration
- Or stop the process using the port:
  ```powershell
  netstat -ano | findstr :8080
  taskkill /PID <PID> /F
  ```

## Configuration

### Files Directory
Edit `Web.config` to set custom files directory:
```xml
<appSettings>
  <add key="DacpacFilesPath" value="C:\DacpacFiles" />
</appSettings>
```

If not set, defaults to `App_Data/Files` relative to application root.

## Verification Checklist

- [ ] Solution builds without errors
- [ ] `DatabaseExtractor.dll` exists in `FileDownloadApi\bin`
- [ ] `FileDownloadApi.dll` exists in `FileDownloadApi\bin`
- [ ] Web.config has `targetFramework="4.8"`
- [ ] IIS Express or IIS is installed
- [ ] API responds to `GET /api/files`
- [ ] All 6 endpoints are accessible

## Default Port

The default port is typically **8080**, but Visual Studio may assign a different port. Check the Output window or browser address bar for the actual port.

