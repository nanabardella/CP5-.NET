# Revisão da entrega CP5

| Requisito | Situação |
| --- | --- |
| Produtos v1 deprecada, array completo | Validado por HTTP com Oracle |
| Produtos v2 padrão, envelope paginado | Validado por HTTP com Oracle |
| Query api-version e header X-Api-Version | Validado por HTTP |
| Headers de versões e Swagger separado | Validado por HTTP |
| Clientes e categorias preservados | Presentes nos dois documentos Swagger |
| Application valida page e pageSize | Implementado e testado |
| Count, OrderBy, Skip e Take no IQueryable | Implementado na Infrastructure |
| Página enorme sem overflow, 200 vazio | Validado por HTTP |
| POST limitado por IP, 10/minuto | Validado: 429 com Retry-After e JSON |
| Health separado do limite | Validado: HTTP 200 após 429 |
| Testes CP4 e paginação | 16 aprovados |
| Oracle sem literais booleanos incompatíveis | Configurado UseOracleSQLCompatibility(DatabaseVersion19) |

## Organização

- `docs/` é a pasta oficial das evidências. Removida a cópia idêntica de `KOVA/docs/health-unhealthy.json`; o original foi preservado em `docs/health-unhealthy.json`.
- `README.md` na raiz do repositório Git (`CP4-.NET`) contém a documentação da entrega. O README externo orienta a navegação no workspace.
- Mantidos DbContext, migrations, entidades, repositórios, controllers, DTOs, handler, health e projetos de testes anteriores.
- `BancoDadosContext.cs` é uma classe legada, apesar do nome não ser um DbContext do EF. Não participa da persistência atual; preservada para não remover conteúdo anterior sem necessidade.
- `bin/`, `obj/`, `.vs/` e logs são gerados e ignorados pelo Git. `.git/` é o histórico do repositório e deve ser preservado.
- Dockerfile ajustado para .NET 9 e para restaurar os projetos referenciados; imagem não validada porque Docker não está disponível neste ambiente.
- Arquivo HTTP corrigido: removida chamada ao weatherforecast inexistente.

## Pendências de entrega

As verificações foram concluídas em 09/10/2026, com Oracle disponível. As capturas reais e a saída dos 16 testes estão em `docs/cp5/`. Páginas distintas e totais foram verificados pelo script.

Falta publicar as alterações no repositório GitHub público do grupo e entregar seu link no portal.

As capturas de falha foram substituídas pela execução completa. O aviso NU1900 não é erro de compilação: indica falha ao consultar vulnerabilidades no NuGet.
