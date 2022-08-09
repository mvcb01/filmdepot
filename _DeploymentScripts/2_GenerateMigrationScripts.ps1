$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = $user + '\entertainmentdepot_PROD'

$solutionRoot = (get-item $PSScriptRoot).parent.FullName

cd ($solutionRoot + '\FilmDataAccess.EFCore')

$outputString = dotnet ef migrations list
$lines = $outputString -split [Environment]::NewLine
foreach ($line in $lines)
{
  # migrations have names like 20220510145828_DummyMigration
  if ($line -match '^[0-9]{14}_')
  {
    echo $line
  }
}

cd $cwd
