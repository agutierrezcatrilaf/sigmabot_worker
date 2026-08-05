# Genera Scripts/Upsert_CodigoDestino_FromExcel_10008.sql desde Excel de homologación.
# TipoDocumento: C=ValorOrigen, F+E=ValorDestino, F=CodigoDestino
# Discipline:   D=ValorOrigen, F=ValorDestino, G=CodigoDestino
# Cwa:          D=ValorOrigen, F=ValorDestino, G=CodigoDestino
param(
    [string]$DownloadsPath = (Join-Path $env:USERPROFILE 'Downloads'),
    [int]$IdTrabajo = 10008,
    [string]$Codelco = '1207996652',
    [string]$Salfa = '1207996803',
    [string]$OutSql = (Join-Path $PSScriptRoot 'Upsert_CodigoDestino_FromExcel_10008.sql')
)

function Escape-Sql([string]$s) {
    if ($null -eq $s) { return '' }
    return ($s -replace "'", "''")
}

function Read-ExcelRows([string]$path, [int]$startRow = 2) {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    try {
        $wb = $excel.Workbooks.Open($path)
        $ws = $wb.Worksheets.Item(1)
        $used = $ws.UsedRange
        $maxRow = $used.Rows.Count
        $rows = [System.Collections.Generic.List[object]]::new()
        for ($r = $startRow; $r -le $maxRow; $r++) {
            $cells = @(for ($c = 1; $c -le 8; $c++) { $ws.Cells.Item($r, $c).Text.Trim() })
            if ([string]::IsNullOrWhiteSpace($cells[2]) -and [string]::IsNullOrWhiteSpace($cells[3])) { continue }
            $rows.Add($cells)
        }
        $wb.Close($false)
        return [object[]]$rows.ToArray()
    }
    finally {
        $excel.Quit()
        [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    }
}

$tipoPath = Join-Path $DownloadsPath 'HomologacionTipoDocumento.xlsx'
$discPath = Join-Path $DownloadsPath 'HomologacionDiscipline.xlsx'
$cwaPath  = Join-Path $DownloadsPath 'HomologacionCWA.xlsx'

if (-not (Test-Path $tipoPath)) { throw "No existe $tipoPath" }
if (-not (Test-Path $discPath)) { throw "No existe $discPath" }
if (-not (Test-Path $cwaPath))  { throw "No existe $cwaPath" }

$sb = New-Object System.Text.StringBuilder
$sb.AppendLine('SET QUOTED_IDENTIFIER ON;') | Out-Null
$sb.AppendLine('SET ANSI_NULLS ON;') | Out-Null
$sb.AppendLine('GO') | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("DECLARE @IdTrabajo INT = $IdTrabajo;") | Out-Null
$sb.AppendLine("DECLARE @Codelco   NVARCHAR(50) = N'$Codelco';") | Out-Null
$sb.AppendLine("DECLARE @Salfa     NVARCHAR(50) = N'$Salfa';") | Out-Null
$sb.AppendLine('') | Out-Null

# TipoDocumento
$tipoRows = Read-ExcelRows $tipoPath
foreach ($cells in $tipoRows) {
    $origen = $cells[2]  # C
    $desc = $cells[4]    # E (index 4 = col E)
    $codF = $cells[5]    # F
    if ([string]::IsNullOrWhiteSpace($origen) -or [string]::IsNullOrWhiteSpace($codF)) { continue }
    $destino = if ([string]::IsNullOrWhiteSpace($desc)) { $codF } else { "$codF-$desc" }
    $o = Escape-Sql $origen
    $d = Escape-Sql $destino
    $c = Escape-Sql $codF
    $sb.AppendLine("UPDATE dbo.TransmittalSyncEquivalencia SET ValorDestino = N'$d', CodigoDestino = N'$c', Activo = 1, UpdatedAt = SYSUTCDATETIME() WHERE IdTrabajo = @IdTrabajo AND ACXProjectIdOrigen = @Codelco AND ACXProjectIdDestino = @Salfa AND Tipo = N'TipoDocumento' AND ValorOrigen = N'$o';") | Out-Null
    $sb.AppendLine("IF @@ROWCOUNT = 0 INSERT INTO dbo.TransmittalSyncEquivalencia (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino, CodigoDestino) VALUES (@IdTrabajo, @Codelco, @Salfa, N'TipoDocumento', N'$o', N'$d', N'$c');") | Out-Null
}

# Discipline: D=ValorOrigen (col 4), F=ValorDestino (col 6), G=CodigoDestino (col 7)
$discRows = Read-ExcelRows $discPath
foreach ($cells in $discRows) {
    $origen = $cells[3]
    $destino = $cells[5]
    $codG = $cells[6]
    if ([string]::IsNullOrWhiteSpace($origen) -or [string]::IsNullOrWhiteSpace($destino) -or [string]::IsNullOrWhiteSpace($codG)) { continue }
    $o = Escape-Sql $origen
    $d = Escape-Sql $destino
    $c = Escape-Sql $codG
    $sb.AppendLine("UPDATE dbo.TransmittalSyncEquivalencia SET ValorDestino = N'$d', CodigoDestino = N'$c', Activo = 1, UpdatedAt = SYSUTCDATETIME() WHERE IdTrabajo = @IdTrabajo AND ACXProjectIdOrigen = @Codelco AND ACXProjectIdDestino = @Salfa AND Tipo = N'Discipline' AND ValorOrigen = N'$o';") | Out-Null
    $sb.AppendLine("IF @@ROWCOUNT = 0 INSERT INTO dbo.TransmittalSyncEquivalencia (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino, CodigoDestino) VALUES (@IdTrabajo, @Codelco, @Salfa, N'Discipline', N'$o', N'$d', N'$c');") | Out-Null
}

# Cwa: D=ValorOrigen (col 4), F=ValorDestino (col 6), G=CodigoDestino (col 7)
$cwaRows = Read-ExcelRows $cwaPath
foreach ($cells in $cwaRows) {
    $origen = $cells[3]
    $destino = $cells[5]
    $codG = $cells[6]
    if ([string]::IsNullOrWhiteSpace($origen) -or [string]::IsNullOrWhiteSpace($destino) -or [string]::IsNullOrWhiteSpace($codG)) { continue }
    $o = Escape-Sql $origen
    $d = Escape-Sql $destino
    $c = Escape-Sql $codG
    $sb.AppendLine("UPDATE dbo.TransmittalSyncEquivalencia SET ValorDestino = N'$d', CodigoDestino = N'$c', Activo = 1, UpdatedAt = SYSUTCDATETIME() WHERE IdTrabajo = @IdTrabajo AND ACXProjectIdOrigen = @Codelco AND ACXProjectIdDestino = @Salfa AND Tipo = N'Cwa' AND ValorOrigen = N'$o';") | Out-Null
    $sb.AppendLine("IF @@ROWCOUNT = 0 INSERT INTO dbo.TransmittalSyncEquivalencia (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino, CodigoDestino) VALUES (@IdTrabajo, @Codelco, @Salfa, N'Cwa', N'$o', N'$d', N'$c');") | Out-Null
}

$sb.AppendLine('') | Out-Null
$sb.AppendLine('SELECT Tipo, COUNT(*) AS Activos FROM dbo.TransmittalSyncEquivalencia WHERE IdTrabajo = @IdTrabajo AND Activo = 1 GROUP BY Tipo ORDER BY Tipo;') | Out-Null
$sb.AppendLine('PRINT ''Upsert_CodigoDestino_FromExcel_10008 completado.'';') | Out-Null
$sb.AppendLine('GO') | Out-Null

[System.IO.File]::WriteAllText($OutSql, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Generado: $OutSql (tipo=$($tipoRows.Count), disc=$($discRows.Count), cwa=$($cwaRows.Count))"
