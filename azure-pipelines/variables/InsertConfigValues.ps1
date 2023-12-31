$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}
$BinPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\bin\Packages\$BuildConfiguration")

$dirsToSearch = "$BinPath\NuGet\*.nupkg" |? { Test-Path $_ }
$icv=@()

if ($dirsToSearch) {
    Get-ChildItem -Path $dirsToSearch |% {
        if ($_.Name -match "^(.*?)\.(\d+\.\d+\.\d+(?:\.\d+)?(?:-.*?)?)(?:\.symbols)?\.nupkg$") {
            $id = $Matches[1]
            $version = $Matches[2]
            $icv += "$id=$version"
        }
    }
}

Write-Output ([string]::join(',',$icv))
