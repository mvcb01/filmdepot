$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

$solutionRoot = (get-item $PSScriptRoot).parent.FullName

echo "-----------"
echo ("Deploying to {0}" -f $deploymentDir)
echo " "

cd $solutionRoot
dotnet publish .\entertainmentdepot.sln -o $deploymentDir -c Release
cd $cwd
