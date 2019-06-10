$openjdk_version = "1.8.0.25"
$download_url = "https://dl.xamarin.com/OpenJDK/mac/microsoft-dist-openjdk-" + $openjdk_version + ".zip"

Write-Host "Installing OpenJDK ..." -ForegroundColor Cyan

$jdkPath = "C:\Program Files\Android\jdk\microsoft_dist_openjdk_" + $openjdk_version

if(Test-Path $jdkPath) {
    Remove-Item $jdkPath -Recurse -Force
}

Write-Host "Downloading..."
$zipPath = "$env:TEMP\microsoft-dist-openjdk-" + $openjdk_version + ".zip"
(New-Object Net.WebClient).DownloadFile($download_url, $zipPath)

Write-Host "Unpacking..."
7z x $zipPath -oC:\openjdk_temp | Out-Null
[IO.Directory]::Move('C:\openjdk_temp', $jdkPath)
del $zipPath

Write-Host "Setting ENV var..."
[Environment]::SetEnvironmentVariable("JAVA_HOME", "C:\Progra~1\Android\jdk\microsoft_dist_openjdk_" + $openjdk_version, "machine")
[Environment]::SetEnvironmentVariable("JAVA_SDK", "C:\Progra~1\Android\jdk\microsoft_dist_openjdk_" + $openjdk_version, "machine")
$env:JAVA_HOME="C:\Progra~1\Android\jdk\microsoft_dist_openjdk_" + $openjdk_version
$env:JAVA_SDK="C:\Progra~1\Android\jdk\microsoft_dist_openjdk_" + $openjdk_version

Add-Path $jdkPath
Add-Path $jdkPath + "\bin"