# Demonstração Azure

O template `infra/main.bicep` e o workflow manual `deploy-azure.yml` são executáveis, mas não foram provisionados sem uma conta Azure configurada. Compilar Bicep valida a estrutura, não disponibilidade regional, quota ou permissões de deployment.

## Recursos e premissas de custo

- App Service Linux Standard S1, uma instância por padrão e slot staging; `instances` permite de 1 a 3.
- Azure SQL Basic, armazenamento máximo de 2 GiB.
- Key Vault Standard com segredo de conexão SQL e identidade gerenciada apenas para leitura do segredo.
- Application Insights com workspace Log Analytics e retenção de 30 dias; volume de telemetria não foi medido.
- Grupo de ação/email e alerta de mais de 5 erros HTTP 5xx em 5 minutos.

Use a região do resource group e o [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) para estimar antes de executar: 730 horas/mês do plano multiplicadas pelas instâncias, SQL Basic, operações do Key Vault, ingestão/retenção de logs e saída de dados. A conta depende da região, moeda, descontos e volume medido. Não há estimativa numérica validada para sua assinatura; não se considera custo zero.

## Configurar GitHub/Azure uma vez

1. Crie um resource group dedicado à demonstração na região desejada.
2. Configure uma identidade de deployment federada com GitHub OIDC, restrita ao repositório e ao environment `azure-demo`. Ela precisa criar recursos e atribuições RBAC no resource group; use o menor escopo possível. A identidade de deployment é diferente da identidade de runtime criada pelo Bicep.
3. No environment `azure-demo`, configure as variáveis `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_APP_NAME` (globalmente único) e `AZURE_ALERT_EMAIL`.
4. Configure `AZURE_SQL_PASSWORD` como secret forte, com pelo menos 16 caracteres. O workflow não imprime o arquivo temporário de parâmetros e o remove no fim.
5. Configure aprovação do environment se desejar controlar criação/custo de recursos. Execute o CI antes do deployment.
6. Execute manualmente **Azure demo deployment** a partir do commit revisado. Não é acionado por push/PR.

O fluxo provisiona, publica um pacote associado ao SHA, abre uma regra temporária para a migração no runner, aplica migrations/seed, remove essa regra, publica no slot, verifica TLS/readiness e faz swap. A credencial SQL vai ao Key Vault; o app usa referência ao segredo com identidade gerenciada. O runtime ainda autentica no banco com SQL authentication. Não se atribui autenticação gerenciada no SQL sem implementar o usuário e suas permissões.

A regra `AllowAzureServices` é uma simplificação explícita de demonstração; permita somente redes/identidades necessárias numa evolução para produção. O template habilita forwarded headers no ambiente gerenciado App Service para identificar HTTPS do proxy. Em hospedagens próprias, configure `ForwardedHeaders:KnownProxies` explicitamente e não confie em headers de clientes diretos.

## Evidências após o primeiro deploy

Registre URL HTTPS, região/SKUs reais, SHA/artifact e link do run. Teste catálogo/carrinho com dados fictícios, preserve cookies ao alternar/reiniciar instâncias, confira a mesma sessão, uma requisição com dependência SQL no Application Insights e a entrega do alerta ao operador. Publique somente capturas sanitizadas; não publique formulários, cookies ou connection strings.

O alerta e a telemetria estão configurados em código; recebimento de email, consultas úteis, escala, backup e recuperação exigem exercício no ambiente real. Defina orçamento/alerta de custo na assinatura antes de deixar a demonstração ativa.

## Rollback Azure

Se o staging falha, o workflow não promove. Após um swap, a versão anterior permanece no staging. Para voltar, verifique antes se ela é compatível com o schema atual e execute:

```bash
az webapp deployment slot swap -g YOUR_RESOURCE_GROUP -n YOUR_APP --slot staging --target-slot production
```

Confira `/health/ready`, o catálogo e uma sessão de teste depois. O rollback local de imagem é testado no CI; swap e recuperação Azure não foram executados sem a conta configurada. Não reverta migrations de forma automática.

## Remoção

A remoção do resource group elimina aplicação, banco e telemetria associados. Faça backup/exportação se houver algo a preservar e execute essa ação apenas para um grupo exclusivamente descartável. Key Vault tem soft delete/retention; isso pode impedir reutilizar o nome imediatamente. Não há tarefa de exclusão automática de recursos.
