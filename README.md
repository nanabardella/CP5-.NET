# CP5 - Versionamento, Paginação e Rate Limit | KOVA

## Integrantes do Grupo

| Nome                           | RM     |
| ------------------------------ | ------ |
| Erick Takeshi Andrade Nakajune | 566059 |
| Giovanna Bardella Gomes        | 561439 |
| Diogo Cunha Abrão de Oliveira  | 563654 |

---

## Domínio do Projeto

O domínio escolhido para o projeto é um **E-commerce de Roupas**.

O projeto KOVA representa uma loja online, modelada com foco no fluxo principal de compra. O sistema permite que clientes visualizem produtos, realizem pedidos e efetuem pagamentos, com as entidades organizadas em camadas seguindo os princípios de Clean Architecture.

---

## Objetivo desta Entrega

Esta entrega evolui o CP4 para o CP5: produtos têm v1 deprecada e v2 paginada, e o POST recebe um limite de taxa. São preservados:

* persistência com Entity Framework Core
* organização em camadas
* criação de DbContext
* mapeamentos com Fluent API
* criação e versionamento de migrations
* implementação de repositórios por contrato
* configuração de injeção de dependência
* uso de connection string de forma segura
* endpoints REST documentados com Swagger/OpenAPI
* repositório genérico e tratamento global de exceções
* health checks do processo e do banco em `GET /health`
* logs estruturados com `traceId`
* testes unitários de Domain e Application com xUnit e Moq

---

## Tecnologias Utilizadas

* .NET
* ASP.NET Core Web API
* Entity Framework Core
* Oracle Entity Framework Core Provider
* Oracle Database

---

## Estrutura da Solução

A solução está organizada em camadas:

* **Kova.Domain**
  Contém as entidades e regras centrais do domínio.

* **Kova.Application**
  Contém as interfaces de repositório e contratos da aplicação.

* **Kova.Infrastructure**
  Contém:

  * DbContext
  * configurações de mapeamento com Fluent API
  * implementações de repositório
  * migrations

* **Kova.api**
  Projeto responsável pela exposição da API, controllers, DTOs de HTTP, Swagger e configuração de DI.

---

## Entidades Modeladas

As principais entidades do sistema são:

* Cliente
* Pedido
* Pagamento
* Produto
* Categoria

---

## Relacionamentos do Sistema

Os relacionamentos foram modelados da seguinte forma:

* Cliente -> Pedido
  Um cliente pode realizar um ou mais pedidos.

* Pedido -> Pagamento
  Todo pedido possui um pagamento associado.

* Produto -> Categoria
  Cada produto possui uma categoria.

* Pedido <-> Produto
  Relacionamento muitos-para-muitos (N:N) mapeado explicitamente.

---

## Persistência com EF Core

A persistência foi implementada na camada Infrastructure, contendo:

* KovaDbContext
* configurações por entidade com IEntityTypeConfiguration
* aplicação automática das configurações com ApplyConfigurationsFromAssembly
* mapeamentos com Fluent API
* migration inicial com o esquema do banco
* repositórios concretos

---

## Repositórios

As interfaces dos repositórios foram definidas na camada Application, e suas implementações foram criadas na camada Infrastructure.

Exemplos:

* IClienteRepository
* ICategoriaRepository
* IProdutoRepository
* IPedidoRepository
* IPagamentoRepository

---

## Injeção de Dependência

O registro de dependências foi configurado no projeto da API por meio do método:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

Esse método centraliza o registro do DbContext e dos repositórios no container de injeção de dependência da aplicação.

O contrato genérico `IRepository<T>` fica na camada Application e a implementação `Repository<T>` fica na Infrastructure. O registro é feito com `AddScoped(typeof(IRepository<>), typeof(Repository<>))`. O fluxo de categorias usa esse contrato para listar, consultar e criar registros; os repositórios específicos continuam disponíveis para consultas com relacionamentos.

## API REST e Swagger

Os endpoints usam DTOs de request/response e não expõem entidades de domínio diretamente:

* `GET /api/categorias`, `GET /api/categorias/{id}` e `POST /api/categorias`
* `GET /api/clientes`, `GET /api/clientes/{id}` e `POST /api/clientes`
* `GET /api/produtos`, `GET /api/produtos/{id}` e `POST /api/produtos`

Depois de iniciar a API, a documentação está disponível em:

`https://localhost:<porta>/swagger`

As actions possuem comentários XML, respostas HTTP declaradas e exemplos de sucesso, validação e recurso não encontrado na interface do Swagger.

## Tratamento global de erros

`GlobalExceptionHandler` implementa `IExceptionHandler` e retorna respostas RFC 7807 com `Content-Type: application/problem+json`:

| Exceção | Status |
| --- | --- |
| `ArgumentException` ou `DomainException` | 400 |
| `ResourceNotFoundException` ou `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| Demais exceções | 500 |

Detalhes internos de erros 500 são exibidos apenas em Development; em outros ambientes a resposta usa uma mensagem genérica.

---

## Banco de Dados Utilizado

O SGBD utilizado neste projeto é:

**Oracle Database**

---

## Connection String

A connection string deve ser configurada no arquivo `appsettings.Development.json` do projeto da API.

Exemplo seguro:

```json
{
  "ConnectionStrings": {
    "KovaOracle": "Data Source=oracle.fiap.com.br:1521/orcl;User ID=<USUARIO>;Password=<SENHA>;"
  }
}
```


## Como Executar o Projeto

1. Restaurar os pacotes

```
dotnet restore
```

2. Compilar a solução

```
dotnet build
```

3. Aplicar a migration no banco

```
dotnet ef database update --project Kova.Infrastructure --startup-project Kova.api
```

4. Executar a API

```
dotnet run --project Kova.api
```

5. Abrir o Swagger

Com a API em execução, acesse `https://localhost:<porta>/swagger` ou a URL HTTP informada pelo terminal.

Exemplo de criação de categoria:

```http
POST /api/categorias
Content-Type: application/json

{
  "nome": "Camisetas",
  "descricao": "Camisetas e peças básicas"
}
```

---

## Migrations

A solução contém migration versionada para criação inicial do schema do banco.

Exemplo de comando para gerar migrations:

```
dotnet ef migrations add InitialCreate --project Kova.Infrastructure --startup-project Kova.api
```

Exemplo de comando para aplicar:

```
dotnet ef database update --project Kova.Infrastructure --startup-project Kova.api
```

---

## Evidências

As evidências complementares da entrega podem ser encontradas na pasta `/docs`, incluindo:

* diagrama do esquema físico
* MER atualizado
* prints do banco após aplicação da migration
* justificativas de ajustes realizados

---

## Observações Finais

O projeto foi estruturado respeitando os princípios de separação por camadas, mantendo a persistência concentrada na Infrastructure, os contratos na Application e a configuração da API no projeto Kova.api.

Nenhuma regra de negócio complexa foi colocada na camada de infraestrutura, mantendo o foco da entrega em persistência, mapeamento e organização arquitetural.

## CP4: Health Checks, Observabilidade e Testes

### Health checks

O endpoint único `GET /health` retorna JSON com o status geral, duração, `traceId` e os checks nomeados `self` e `database`. O check `self` confirma que o processo está ativo e `database` usa `AddDbContextCheck<KovaDbContext>` para verificar o Oracle do CP2. Respostas `Healthy` e `Degraded` usam HTTP 200; `Unhealthy` usa HTTP 503. Detalhes de exceções do banco aparecem somente em Development.

Exemplos de respostas estão em `docs/health-healthy.json` e `docs/health-unhealthy.json`.

### Logs estruturados

O POST de produtos registra início e sucesso com propriedades nomeadas (`CategoriaId`, `ProdutoId`) e `HttpContext.TraceIdentifier`. O `GlobalExceptionHandler` registra exceções em nível Error com o mesmo `traceId`; em Development esse identificador também aparece no `ProblemDetails`.

### Testes automatizados

Os projetos `Kova.Domain.Tests` e `Kova.Application.Tests` fazem parte da solução. O primeiro testa as regras de `Categoria` sem mocks, com `[Fact]` e `[Theory]`; o segundo testa `ProdutoService` com Moq, verificando que uma categoria inexistente lança `ResourceNotFoundException` sem chamar `Add` ou `SaveChanges`.

Execute todos os testes a partir da pasta `KOVA`:

```powershell
dotnet test
```

Os logs JSON no console registram criação de produtos e exceções com `traceId`. Durante a captura automatizada, os logs são salvos em `docs/cp5/api-stdout.log` e `api-stderr.log` para diagnóstico local.

## CP5: contratos, paginação e limite de taxa

O recurso escolhido é **produtos**. As duas versões usam `IProdutoService`/`ProdutoService`; clientes e categorias são `ApiVersionNeutral` e continuam disponíveis nos dois documentos Swagger. GET por id e POST de produtos funcionam em ambas as versões, inclusive sem versão (2.0).

A partir de `KOVA`, configure a conexão Oracle por variável de ambiente (não publique credenciais), aplique a migration existente e execute:

```powershell
$env:ConnectionStrings__KovaOracle = 'Data Source=<HOST>:1521/<SERVICE>;User ID=<USUARIO>;Password=<SENHA>;'
dotnet restore
dotnet ef database update --project Kova.Infrastructure --startup-project Kova.api
dotnet run --project Kova.api --launch-profile http
```

| Chamada | URL |
| --- | --- |
| Swagger (Development, grupos v1.0 deprecado e v2.0) | http://localhost:5070/swagger |
| Health | http://localhost:5070/health |
| v1: array completo, deprecada | http://localhost:5070/api/produtos?api-version=1.0 |
| v2: envelope paginado | http://localhost:5070/api/produtos?api-version=2.0&page=1&pageSize=20 |
| Sem versão: usa v2 | http://localhost:5070/api/produtos |

Também é possível escolher a versão com `X-Api-Version: 1.0` ou `X-Api-Version: 2.0`. As respostas informam `api-supported-versions` e `api-deprecated-versions`. A v1 mantém a lista inteira; a v2 retorna `page`, `pageSize`, `totalItems`, `totalPages` e `items`.

`page` tem padrão 1 e deve ser >= 1. `pageSize` tem padrão 20 e deve estar entre 1 e 100. A Application valida antes de consultar o repositório; valores inválidos geram 400 Problem Details com a regra. Páginas além do total, inclusive `page=2147483647`, retornam 200 com `items: []`. O repositório executa Count e paginação no IQueryable com OrderBy(CreatedAt), ThenBy(Id), Skip e Take antes de ToList. Total de páginas = teto de totalItems/pageSize.

`POST /api/produtos` usa a política `produto-write`: **10 requisições por IP por janela fixa de 60 segundos**, sem fila. A 11ª requisição na mesma janela recebe **429**, `Retry-After` em segundos e JSON Problem Details. Requisições rejeitadas por validação também consomem permissões. GETs não usam essa política. `/health` mantém os checks de processo e Oracle e não divide esse teto; com Oracle indisponível continua retornando 503 conforme CP4.

```powershell
cd KOVA
dotnet test
```

Os testes de Domain e Application do CP4 são preservados; `PaginationTests` verifica limites inválidos sem consultar o repositório e mapeamento de itens/totais nas páginas válidas. Use `docs/capturar-cp5.ps1` com a API e Oracle ativos para gravar evidências HTTP reais. O script cria categoria e quatro produtos de demonstração; use banco de desenvolvimento. Evidências não executadas não são apresentadas como resultados reais.

### Captura automatizada da entrega

Na raiz do repositório (`CP4-.NET`), execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\docs\concluir-cp5.ps1
```

O script pede usuário e senha no terminal e mantém a conexão somente no ambiente do processo. Inicia a API na porta **5071**, verifica o banco, cria os dados de demonstração e captura os contratos, páginas, erros 400, Swagger, headers, 429 e health após o limite. Executa os testes e atualiza `docs/cp5/README.md` como concluído somente quando todas as verificações passam. Acrescente `-AplicarMigration` se o schema ainda precisar das tabelas (requer `dotnet-ef` compatível com EF Core 9).

O provider usa `UseOracleSQLCompatibility(DatabaseVersion19)` para gerar SQL compatível com Oracle 19c e evitar o erro ORA-00904 em literais booleanos. Não é necessário alterar as migrations existentes para essa configuração.

São **16 testes** (5 Domain e 11 Application), com saída real em `docs/dotnet-test-output.txt`. A organização dos arquivos, os requisitos atendidos e as pendências estão em [docs/revisao-entrega.md](docs/revisao-entrega.md).

A entrega no portal é o link do **repositório GitHub público do grupo**, contendo a evolução do CP4, README e evidências. Os diretórios gerados `bin`, `obj`, `.vs` e logs de diagnóstico ficam fora do Git; `.git` deve ser preservado.
