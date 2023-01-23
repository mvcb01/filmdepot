$currentBranchName = git branch --show-current
if ($currentBranchName -ne "main" -And $currentBranchName -ne "master")
{
    echo ("Current branch not main or master: " + $currentBranchName)
    exit
}

# ------

$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

$solutionRoot = (get-item $PSScriptRoot).parent.FullName

echo "-----------"
echo ("Deploying to {0}" -f $deploymentDir)
echo " "

cd $solutionRoot
dotnet publish .\filmdepot.sln -o $deploymentDir -c Release

# creates the warehouse contents dir if not exists
$migrationsDir = Join-Path -Path $deploymentDir -ChildPath 'whcontents'
[System.IO.Directory]::CreateDirectory($migrationsDir) | out-null

cd $cwd
