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

cd ($solutionRoot + '\FilmDataAccess.EFCore')

# creates the Migration dir if not exists; no need to print anything to the console
$migrationsDir = Join-Path -Path $deploymentDir -ChildPath 'Migrations'
[System.IO.Directory]::CreateDirectory($migrationsDir) | out-null

$outputString = dotnet ef migrations list
$lines = $outputString -split [Environment]::NewLine
$existingMigrations = Get-ChildItem -Path $migrationsDir | Select -ExpandProperty Name
$migrationFrom = '0'
$migrationTo = ''

foreach ($line in $lines)
{
  # migrations have names like 20220510145828_SomeMigrationName
  if ($line -match '^[0-9]{14}_')
  {
    $migrationTo = $line.Trim()
    $filename = $migrationTo + ".sql"
    $filePath = Join-Path -Path $migrationsDir -ChildPath $filename

    if ($existingMigrations -Contains $filename)
    {
      echo ("Migration already exists: " + $filePath)
    }
    else
    {
      echo ("Generating " + $filePath)
      dotnet ef migrations script $migrationFrom $migrationTo -o $filePath
    }

    $migrationFrom = $migrationTo
  }
}

cd $cwd
