$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

$solutionRoot = (get-item $PSScriptRoot).parent.FullName

cd ($solutionRoot + '\FilmDataAccess.EFCore')

$outputString = dotnet ef migrations list
$lines = $outputString -split [Environment]::NewLine
$migrationFrom = '0'
$migrationTo = ''
foreach ($line in $lines)
{
  # migrations have names like 20220510145828_SomeMigrationName
  if ($line -match '^[0-9]{14}_')
  {
    $migrationTo = $line.Trim()
    $filePath = Join-Path -Path $deploymentDir -ChildPath 'Migrations' | Join-Path -ChildPath ($migrationTo + ".sql")
    echo ("Generating " + $filePath)
    dotnet ef migrations script $migrationFrom $migrationTo -o $filePath
    $migrationFrom = $migrationTo
  }
}

cd $cwd
