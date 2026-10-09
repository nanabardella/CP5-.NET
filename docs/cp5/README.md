# Evidências CP5 concluídas

Capturas reais obtidas em 2026-10-09T02:46:09.5097813-03:00 a partir de http://localhost:5071.

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

Os arquivos `v1-query.json`, `v1-header.json`, `v2-default.json`, `v2-page1.json`, `v2-page2.json` e `v2-empty.json` contêm os corpos extraídos das capturas HTTP, sem alterar os dados. `swagger-trecho.md` destaca os dois grupos e a depreciação da v1; não é necessário print adicional para o requisito que aceita print ou trecho.
