$param0 = $args[0]
if (!$args[0]) { 
    Write-Host "Version must be provided."
    exit 1
}
Write-Host "Deploy Version: $param0"

dotnet build --configuration Release
dotnet pack --configuration Release
dotnet nuget push "ControlBee/bin/Release/ControlBee.$param0.nupkg" --source "github"
