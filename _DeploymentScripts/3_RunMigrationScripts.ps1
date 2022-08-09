$cwd = (Get-Item .).FullName

$user = $env:USERPROFILE
$deploymentDir = Join-Path -Path $user -ChildPath 'entertainmentdepot_PROD'

cd $deploymentDir

# creates the backup dir if not exists; no need to print anything to the console
$backups = Join-Path -Path $deploymentDir -ChildPath 'backups'
[System.IO.Directory]::CreateDirectory($backups) | out-null

$dbExists = Test-Path -Path FilmDb.db -PathType Leaf

if ($dbExists)
{
    # TODO: find all the migration scripts not executed yet using table __EFMigrationsHistory
    echo "db exists..."
}
else
{
    echo "Database FilmDb.db does not exist, creating..."
    New-Item -Path FilmDb.db -ItemType File | out-null

    echo "Running all migrations:"
    $dirContentsString = ls .\Migrations
    $dirContents = $dirContentsString -split [Environment]::NewLine
    foreach ($file in $dirContents)
    {
        if ($file -match '^[0-9]{14}_' -And $file -match '.sql$')
        {
            $scriptPath = Join-Path -Path $deploymentDir -ChildPath 'Migrations' | Join-Path -ChildPath $file
            echo $file
            Get-Content $scriptPath -Raw | sqlite3 .\FilmDb.db
        }
    }

}


cd $cwd
