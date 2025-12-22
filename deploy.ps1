param (
    [switch] $up_version,
    [switch] $major,
    [switch] $minor,
    [switch] $build
)
$ErrorActionPreference = "Stop"

$Verfile = "version"
$fileContents  = (Get-Content $Verfile | Select -Last 1)

if($up_version)
{
    $version = [Version]$fileContents
    if($major) {
        $newVersionString = (New-Object -TypeName 'System.Version' -ArgumentList @(($version.Major+1), 0, 0)).ToString()
    }
    elseif($minor) {
        $newVersionString = (New-Object -TypeName 'System.Version' -ArgumentList @($version.Major, ($version.Minor+1), 0)).ToString()
    }
    elseif($build) {
        $newVersionString = (New-Object -TypeName 'System.Version' -ArgumentList @($version.Major, ($version.Minor), ($version.Build+1))).ToString()
    }
    else {
        throw "Please specify which part of the version should be updated."
    }
    $fileContents = $newVersionString
    Write-Output $fileContents > $Verfile
}
$versionString = $fileContents

Write-Host "Deploy Version: $versionString"

dotnet pack --configuration Release -p:PackageVersion="$versionString"

git add $Verfile
git commit -m "Bump up version to $versionString"

$tagName = "v$versionString"
git tag $tagName

git push
git push origin tag $tagName

dotnet nuget push "ControlBee/bin/Release/ControlBee.$versionString.nupkg" --source "Hotbee"
