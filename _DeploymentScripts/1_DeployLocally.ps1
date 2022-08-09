
$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = $user + '\entertainmentdepot_PROD'

$solutionRoot = (get-item $PSScriptRoot).parent.FullName

echo "-----------"
echo ("Deploying to {0}" -f $deploymentDir)
echo " "

cd $solutionRoot
dotnet publish .\entertainmentdepot.sln -o $deploymentDir -c Release
cd $cwd

