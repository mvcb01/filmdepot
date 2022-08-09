$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

cd $deploymentDir

mkdir .\backups

cd $cwd
