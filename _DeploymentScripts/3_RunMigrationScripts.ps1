$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

cd $deploymentDir

$migrationsDir = Join-Path -Path $deploymentDir -ChildPath 'Migrations'
$migrationsInDir = Get-ChildItem -Path $migrationsDir | Select -ExpandProperty Name
$migrationsToRun = @()

$dbExists = Test-Path -Path FilmDb.db -PathType Leaf
if ($dbExists)
{
    $migrationsHistory = sqlite3 .\FilmDb.db "select MigrationId from __EFMigrationsHistory order by MigrationId desc"

    foreach ($file in $migrationsInDir)
    {
        $isMigrationScript = $file -match '^[0-9]{14}_' -And $file -match '.sql$'
        if (-not $isMigrationScript) { continue }

        $migrationName = [io.path]::GetFileNameWithoutExtension($file)
        if ($migrationsHistory -Contains $migrationName)
        {
            echo ("Migration already ran: " + $file)
        }
        else
        {
            $fullPath = Join-Path -Path $migrationsDir -ChildPath $file
            $migrationsToRun += $fullPath
        }
    }
}
else
{
    echo "Database FilmDb.db does not exist, creating..."
    New-Item -Path FilmDb.db -ItemType File | out-null

    foreach ($file in $migrationsInDir)
    {
        $isMigrationScript = $file -match '^[0-9]{14}_' -And $file -match '.sql$'
        if (-not $isMigrationScript) { continue }

        $fullPath = Join-Path -Path $migrationsDir -ChildPath $file
        $migrationsToRun += $fullPath
    }
}

if ($migrationsToRun.Count -ne 0)
{
    # creates the backup dir if not exists; no need to print anything to the console
    $backups = Join-Path -Path $deploymentDir -ChildPath 'backups'
    [System.IO.Directory]::CreateDirectory($backups) | out-null

    $dateStr = '{0:yyyyMMdd}' -f $(Get-Date -f "yyyyMMddTHHmmss")
    $dbBackup = ".\backups\FilmDb_{0}.db" -f $dateStr
    echo ("Backing up .\FilmDb --> {0} " -f $dbBackup)
    Copy-Item .\FilmDb.db -Destination $dbBackup

    foreach ($migrationPath in $migrationsToRun)
    {
        echo ("Running migration: " + $migrationPath)
        Get-Content $migrationPath -Raw | sqlite3 .\FilmDb.db
    }
}

cd $cwd
