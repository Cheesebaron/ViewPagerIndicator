$download_url = "https://dl.xamarin.com/OpenJDK/mac/microsoft-dist-openjdk-1.8.0.25.zip"

Write-Host "Installing OpenJDK ..." -ForegroundColor Cyan

$jdkPath = 'C:\Program Files\OpenJDK'

if(Test-Path $jdkPath) {
    Remove-Item $jdkPath -Recurse -Force
}

Write-Host "Downloading..."
$zipPath = "$env:TEMP\microsoft-dist-openjdk-1.8.0.25.zip"
(New-Object Net.WebClient).DownloadFile($download_url, $zipPath)

Write-Host "Unpacking..."
7z x $zipPath -oC:\openjdk_temp | Out-Null
[IO.Directory]::Move('C:\openjdk_temp', $jdkPath)
del $zipPath

Write-Host "Setting ENV var..."
[Environment]::SetEnvironmentVariable("JAVA_HOME", "C:\Progra~1\OpenJDK", "machine")
[Environment]::SetEnvironmentVariable("JAVA_SDK", "C:\Progra~1\OpenJDK", "machine")
$env:JAVA_HOME="C:\Progra~1\OpenJDK"
$env:JAVA_SDK="C:\Progra~1\OpenJDK"

Add-Path 'C:\Program Files\OpenJDK\bin'