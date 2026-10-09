param(
    [string]$DataSource = 'oracle.fiap.com.br:1521/orcl',
    [switch]$AplicarMigration
)

$ErrorActionPreference = 'Stop'
$solutionDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../KOVA'))
$apiProject = Join-Path $solutionDirectory 'Kova.api/Kova.api.csproj'
$previousConnection = $env:ConnectionStrings__KovaOracle
$apiProcess = $null
$secretPointer = [IntPtr]::Zero

try {
    if (!$env:ConnectionStrings__KovaOracle) {
        [string]$oracleUser = Read-Host 'Usuário Oracle'
        if ([string]::IsNullOrWhiteSpace($oracleUser)) { throw 'Informe o usuário Oracle.' }
        $oracleSecret = Read-Host 'Senha Oracle (oculta)' -AsSecureString
        $secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($oracleSecret)
        $connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
        $connectionBuilder.set_Item('Data Source', [string]$DataSource)
        $connectionBuilder.set_Item('User ID', [string]$oracleUser)
        $connectionBuilder.set_Item('Password', [string][Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer))
        $env:ConnectionStrings__KovaOracle = $connectionBuilder.get_ConnectionString()
        $connectionBuilder.Clear()
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer)
        $secretPointer = [IntPtr]::Zero
    }

    Push-Location $solutionDirectory
    try {
        dotnet build $apiProject -m:1 -nr:false --disable-build-servers
        if ($LASTEXITCODE -ne 0) { throw 'Compilação falhou.' }
        if ($AplicarMigration) {
            dotnet ef database update --project Kova.Infrastructure --startup-project Kova.api
            if ($LASTEXITCODE -ne 0) { throw 'Migration falhou. Confira conexão e instalação do dotnet-ef.' }
        }

        # Porta separada para não atingir uma API já aberta pelo usuário.
        $baseUrl = 'http://localhost:5071'
        $listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 5071)
        try { $listener.Start() } finally { $listener.Stop() }
        $apiDll = Join-Path $solutionDirectory 'Kova.api/bin/Debug/net9.0/Kova.api.dll'
        $logDirectory = Join-Path $PSScriptRoot 'cp5'
        New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
        $apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @("`"$apiDll`"", '--urls', $baseUrl, '--environment', 'Development') -WorkingDirectory (Join-Path $solutionDirectory 'Kova.api') -RedirectStandardOutput (Join-Path $logDirectory 'api-stdout.log') -RedirectStandardError (Join-Path $logDirectory 'api-stderr.log') -WindowStyle Hidden -PassThru
        $ready = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            if ($apiProcess.HasExited) {
                $startupError = Get-Content -LiteralPath (Join-Path $logDirectory 'api-stderr.log') -Raw -ErrorAction SilentlyContinue
                if ($startupError -match '0x800711C7') {
                    throw 'O Controle de Aplicativo do Windows bloqueou Kova.Infrastructure.dll (0x800711C7). A política do Windows precisa autorizar esse assembly antes de executar a API. Consulte docs/cp5/api-stderr.log.'
                }
                throw 'A API encerrou antes de ficar disponível. Consulte docs/cp5/api-stderr.log e api-stdout.log para identificar a causa.'
            }
            try {
                $response = Invoke-WebRequest "$baseUrl/swagger/v2.0/swagger.json" -UseBasicParsing -TimeoutSec 1
                if ($response.StatusCode -eq 200) { $ready = $true; break }
            } catch { Start-Sleep -Milliseconds 500 }
        }
        if (!$ready) { throw 'A API não iniciou na porta 5071.' }
        $health = Invoke-WebRequest "$baseUrl/health" -UseBasicParsing -TimeoutSec 20
        if ($health.StatusCode -ne 200) { throw 'Oracle indisponível. Confira seus dados de acesso e o schema.' }
        & (Join-Path $PSScriptRoot 'capturar-cp5.ps1') -BaseUrl $baseUrl
        Write-Host 'Concluído: evidências reais e testes gravados em docs/cp5.'
    } finally { Pop-Location }
} finally {
    if ($apiProcess -and !$apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id }
    if ($secretPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer) }
    $env:ConnectionStrings__KovaOracle = $previousConnection
}
