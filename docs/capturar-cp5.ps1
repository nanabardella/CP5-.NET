param([string]$BaseUrl = 'http://localhost:5070')
$ErrorActionPreference = 'Stop'
$outputDirectory = Join-Path $PSScriptRoot 'cp5'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
function Capture($name, $path, $method = 'GET', $body = $null, $headers = @{}) {
    $parameters = @{ Uri = "$BaseUrl$path"; Method = $method; Headers = $headers; UseBasicParsing = $true }
    if ($body) { $parameters.Body = $body; $parameters.ContentType = 'application/json' }
    try {
        $response = Invoke-WebRequest @parameters
        $status = [int]$response.StatusCode
        $content = $response.Content
        $responseHeaders = $response.Headers
    } catch {
        if (!$_.Exception.Response) { throw }
        $status = [int]$_.Exception.Response.StatusCode
        $responseHeaders = $_.Exception.Response.Headers
        $content = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($content)) {
            $errorStream = $_.Exception.Response.GetResponseStream()
            if ($errorStream) {
                $reader = New-Object IO.StreamReader($errorStream)
                try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
            }
        }
    }
    $headerLines = foreach ($key in $responseHeaders.Keys) { "${key}: $($responseHeaders[$key] -join ', ')" }
    $record = "HTTP $status`n$($headerLines -join "`n")`n`n$content"
    [IO.File]::WriteAllText((Join-Path $outputDirectory "$name.txt"), $record)
    return @{ Status = $status; Content = $content; Headers = $responseHeaders }
}
function AssertStatus($response, $expected, $name) {
    if ($response.Status -ne $expected) { throw "$name retornou HTTP $($response.Status); esperado $expected." }
}
$suffix = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$category = Capture 'categoria-criada' '/api/categorias' 'POST' (@{ nome = "CP5-$suffix"; descricao = 'Evidencias CP5' } | ConvertTo-Json)
if ($category.Status -ne 201) { throw 'Não foi possível criar a categoria. Confira Oracle e API.' }
$categoryId = ($category.Content | ConvertFrom-Json).id
1..4 | ForEach-Object {
    $product = Capture "produto-$_" '/api/produtos' 'POST' (@{ nome = "CP5-$suffix-$_"; descricao = 'Produto de demonstracao'; preco = 50; categoriaId = $categoryId } | ConvertTo-Json)
    if ($product.Status -ne 201) {
        if ($product.Status -eq 429) { throw 'Limite de requisições atingido. Aguarde a renovação da janela e tente novamente.' }
        throw "Produto retornou HTTP $($product.Status): $($product.Content). Consulte docs/cp5/produto-$_.txt e api-stderr.log."
    }
}
$v1Query = Capture 'v1-query' '/api/produtos?api-version=1.0'
$v1Header = Capture 'v1-header' '/api/produtos' 'GET' $null @{ 'X-Api-Version' = '1.0' }
$v2 = Capture 'v2-default' '/api/produtos'
AssertStatus $v1Query 200 'v1 por query'
AssertStatus $v1Header 200 'v1 por header'
AssertStatus $v2 200 'v2 por omissão'
if (!$v1Query.Content.TrimStart().StartsWith('[') -or !$v1Header.Content.TrimStart().StartsWith('[')) { throw 'v1 deve retornar um array.' }
$defaultPage = $v2.Content | ConvertFrom-Json
if ($defaultPage.page -ne 1 -or $defaultPage.pageSize -ne 20 -or !$defaultPage.PSObject.Properties['items']) { throw 'Contrato padrão da v2 incorreto.' }
foreach ($response in @($v1Query, $v1Header, $v2)) {
    if ($response.Headers['api-supported-versions'] -notmatch '2.0' -or $response.Headers['api-deprecated-versions'] -notmatch '1.0') { throw 'Headers de versões ausentes ou incorretos.' }
}
$first = Capture 'v2-page1' '/api/produtos?page=1&pageSize=2'
$second = Capture 'v2-page2' '/api/produtos?page=2&pageSize=2'
if ($first.Status -ne 200 -or $second.Status -ne 200) { throw 'Listagem paginada falhou.' }
$page1 = $first.Content | ConvertFrom-Json
$page2 = $second.Content | ConvertFrom-Json
if (@($page2.items | Where-Object { $_.id -in $page1.items.id }).Count -gt 0) { throw 'Páginas se sobrepõem.' }
if ($page1.totalPages -ne [Math]::Ceiling($page1.totalItems / 2.0)) { throw 'Total de páginas incorreto.' }
AssertStatus (Capture 'v2-invalid-page' '/api/produtos?page=0') 400 'page inválido'
AssertStatus (Capture 'v2-invalid-size' '/api/produtos?pageSize=9999') 400 'pageSize inválido'
$empty = Capture 'v2-empty' '/api/produtos?page=2147483647&pageSize=100'
AssertStatus $empty 200 'Página além do total'
if (@(($empty.Content | ConvertFrom-Json).items).Count -ne 0) { throw 'Página além do total deve estar vazia.' }
$swagger1 = Capture 'swagger-v1' '/swagger/v1.0/swagger.json'
$swagger2 = Capture 'swagger-v2' '/swagger/v2.0/swagger.json'
AssertStatus $swagger1 200 'Swagger v1'
AssertStatus $swagger2 200 'Swagger v2'
if (($swagger1.Content | ConvertFrom-Json).info.description -notmatch 'deprecada') { throw 'Swagger v1 deve indicar depreciação.' }
foreach ($swagger in @($swagger1, $swagger2)) {
    $paths = ($swagger.Content | ConvertFrom-Json).paths
    if (!$paths.PSObject.Properties['/api/clientes'] -or !$paths.PSObject.Properties['/api/categorias']) { throw 'Recursos anteriores ausentes no Swagger.' }
}
AssertStatus (Capture 'swagger-ui' '/swagger/index.html') 200 'Swagger UI'
$limited = $false
1..11 | ForEach-Object {
    $result = Capture "rate-limit-$_" '/api/produtos' 'POST' '{}'
    if ($result.Status -eq 429) {
        if (!$result.Headers['Retry-After']) { throw '429 sem Retry-After.' }
        if (($result.Content | ConvertFrom-Json).status -ne 429) { throw 'Corpo do 429 inválido.' }
        $limited = $true
    }
}
if (!$limited) { throw '429 não observado; repita dentro da mesma janela.' }
$health = Capture 'health-after-429' '/health'
if ($health.Status -ne 200) { throw 'Health não retornou 200. Confira a disponibilidade do Oracle; health não tem rate limit.' }
Push-Location (Join-Path $PSScriptRoot '../KOVA')
try {
    dotnet test -m:1 -nr:false --disable-build-servers 2>&1 | Tee-Object -FilePath (Join-Path $outputDirectory 'dotnet-test.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Testes falharam.' }
} finally { Pop-Location }
$summary = @"
# Evidências CP5 concluídas

Capturas reais obtidas em $([DateTimeOffset]::Now.ToString('o')) a partir de $BaseUrl.

- v1 por query e header: HTTP 200 com array e headers de versões.
- v2 sem versão: HTTP 200 com envelope, page 1 e pageSize 20.
- Páginas 1 e 2: HTTP 200, itens sem sobreposição e totalPages coerente.
- page e pageSize inválidos: HTTP 400.
- Página além do total: HTTP 200 com items vazio.
- Swagger v1 e v2: HTTP 200, v1 deprecada, clientes e categorias preservados.
- POST limitado: HTTP 429 com Retry-After e corpo JSON.
- Health após 429: HTTP 200.
- Testes: executados com sucesso; saída em dotnet-test.txt.

As respostas HTTP completas estão nos arquivos desta pasta.
"@
[IO.File]::WriteAllText((Join-Path $outputDirectory 'README.md'), $summary)
