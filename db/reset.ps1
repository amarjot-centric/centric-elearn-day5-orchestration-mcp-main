# Loads db/schema.sql and db/seed.sql into MySQL and prints the row count of every table.
# Settings (environment variables): LEARNHUB_DB_HOST, LEARNHUB_DB_PORT, LEARNHUB_DB_USER, LEARNHUB_DB_PASSWORD
$ErrorActionPreference = 'Stop'

$dbHost   = if ($env:LEARNHUB_DB_HOST)     { $env:LEARNHUB_DB_HOST }     else { '127.0.0.1' }
$dbPort   = if ($env:LEARNHUB_DB_PORT)     { $env:LEARNHUB_DB_PORT }     else { '3306' }
$dbUser   = if ($env:LEARNHUB_DB_USER)     { $env:LEARNHUB_DB_USER }     else { 'root' }
$dbPass   = if ($env:LEARNHUB_DB_PASSWORD) { $env:LEARNHUB_DB_PASSWORD } else { 'Root123$' }
$here     = Split-Path -Parent $MyInvocation.MyCommand.Path

$env:MYSQL_PWD = $dbPass
$common = @('--protocol=TCP', '-h', $dbHost, '-P', $dbPort, '-u', $dbUser, '--default-character-set=utf8mb4')

function Invoke-SqlFile([string]$file, [string]$database) {
    $args2 = $common + @($database) | Where-Object { $_ }
    Get-Content -Raw -Encoding UTF8 $file | & mysql @args2
    if ($LASTEXITCODE -ne 0) { throw "mysql failed while loading $file" }
}

Write-Host "Loading schema into ${dbHost}:${dbPort} ..."
Invoke-SqlFile (Join-Path $here 'schema.sql') ''
Write-Host 'Loading seed data ...'
Invoke-SqlFile (Join-Path $here 'seed.sql') 'learnhub'

Write-Host 'Row counts:'
$tables = 'instructors','students','courses','course_prerequisites','lessons','quiz_questions','enrollments','lesson_progress','bookmarks','cart_items','coupons','orders','order_lines','refunds','reviews','questions','answers','assignments','submissions'
foreach ($t in $tables) {
    $n = & mysql @common -N -B learnhub -e "SELECT COUNT(*) FROM $t"
    '  {0,-22} {1}' -f $t, $n
}
