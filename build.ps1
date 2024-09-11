param (
    [string]$Version = ""
)

$success = $true

if (Get-Process -Name "powertoys" -ErrorAction SilentlyContinue) {
    Write-Host "Stopping PowerToys..."
    $restart_needed = $true
    Stop-Process -Name "powertoys"
}
else {
    $restart_needed = $false
}

if ($?) {
    Write-Host "`nStarting build...`n"

    if ($Version -ne "") {
        Write-Host "Updating release version to $Version..."

        $plugin_json = Get-Content -Path ".\TimeTracker\plugin.json" -Raw | ConvertFrom-Json
        $plugin_json.Version = $Version
        $plugin_json | ConvertTo-Json | Set-Content -Path ".\TimeTracker\plugin.json"

        Write-Host "Updated version number in plugin.json."

        [xml]$project_xml = Get-Content -Path ".\TimeTracker\TimeTracker.csproj"
        $project_xml.Project.PropertyGroup.Version = $Version
        $project_xml.Save(".\TimeTracker\TimeTracker.csproj")

        Write-Host "Updated verion number in project-file.`n"
    }

    dotnet build .\TimeTracker.sln /target:TimeTracker /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Release /p:Platform="x64"

    if ($?) {
        $source = ".\TimeTracker\bin\x64\Release\net8.0-windows"
        $destination = $source + "\TimeTracker"
        $plugin_folder = $env:LOCALAPPDATA + "\Microsoft\PowerToys\PowerToys Run\Plugins\TimeTracker\"

        if (Test-Path -Path $destination) {
            Remove-Item $destination -Recurse
        }
        mkdir $destination | Out-Null

        Copy-Item $source"\plugin.json" $destination"\plugin.json"
        Copy-Item $source"\Community.PowerToys.Run.Plugin.TimeTracker.dll" $destination"\Community.PowerToys.Run.Plugin.TimeTracker.dll"
        Copy-Item $source"\util" $destination"\util" -Recurse
        Copy-Item $source"\icons" $destination"\icons" -Recurse
        Copy-Item ".\README.md" $destination"\README.md"

        Write-Host "`nCopying data into plugins folder..."

        if (Test-Path -Path $plugin_folder) {
            Write-Host "Removing old plugin version..."
            Remove-Item $plugin_folder -Recurse
        }
        Copy-Item $destination $plugin_folder -Recurse

        Write-Host "Copied new release into plugin folder."
        Write-Host "`nCreating release-archive..."

        if (Test-Path -Path "Release.zip") {
            Write-Host "Removing old release-archive..."
            Remove-Item "Release.zip"
        }

        Compress-Archive -Path $destination -DestinationPath "Release.zip"

        Write-Host "Release-archive created."

        Remove-Item $destination -Recurse

        Write-Host "`nBuild complete."
    }
    else {
        Write-Host "`nBuild was unsuccessful."
        $success = $false
    }

    if ($restart_needed) {
        Write-Host "`nRestarting PowerToys..."
        Start-Process -FilePath "C:\Program Files\PowerToys\PowerToys.exe"
    }
}
else {
    Write-Host "`nCouldn't stop PowerToys. Abort build."
    $success = $false
}

if ($success -eq $false) {
    exit 1
}