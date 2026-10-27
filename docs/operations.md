# Operação e recuperação

## Inicialização e migrations

O servidor não cria schema nem importa catálogo ao iniciar. Execute `--migrate` e depois `--seed` em um job único, usando a mesma versão publicada. O seed adiciona produtos ausentes por código; não altera pedidos nem sobrescreve preços de produtos existentes. Não execute vários seeds concorrentes; o índice único é o último guardião.

As migrations novas fazem preflight de duplicatas e dados fora dos limites. Resolva cada divergência explicitamente, após backup, sem truncar/apagar dados para forçar a atualização.

### Base legada criada com EnsureCreated

1. Interrompa escritas no ambiente a migrar e gere backup verificável.
2. Restaure uma cópia de teste. Compare tabelas, colunas, tipos, índices, FKs e dados com o schema até `20190129000045_Modelo`.
3. Gere e revise o SQL de migrations, usando `dotnet-ef` compatível com EF Core 10.0.14.
4. Somente após comprovar equivalência do schema histórico, um operador pode estabelecer o baseline de histórico nessa cópia. Não há script que marque migrations como aplicadas sem verificação.
5. Aplique a nova migration na cópia e execute os testes de leitura, carrinho, cadastro e seed. Valide rollback da aplicação e registre tempos/resultado.
6. Planeje a janela e aplique o procedimento revisado no banco original. A documentação não presume que toda base legada tenha schema idêntico.

## Sinais operacionais

- `/health/live` verifica o processo; não depende de SQL.
- `/health/ready` acessa catálogo, tabela de sessão e chaves; falhas retornam indisponível.
- Logs HTTP incluem método, path sem query string, status, duração e TraceId. Formulários, cookies e dados pessoais não são gravados explicitamente pelo middleware.
- OpenTelemetry instrumenta HTTP e SQL. `APPLICATIONINSIGHTS_CONNECTION_STRING` habilita Azure Monitor; alternativamente `OTEL_EXPORTER_OTLP_ENDPOINT` habilita exportação OTLP. Sem exporter, os logs continuam disponíveis, sem fingir um dashboard implantado.
- Um alerta 5xx foi incluído no Bicep. Um processo vivo não garante fluxo funcional: confirme também um carrinho sintético sem dados reais.

## Rollback local testável

Num Compose descartável, `python3 scripts/smoke.py --rollback` salva a imagem conhecida, cria/inicia uma imagem deliberadamente quebrada, comprova a falha, executa `scripts/rollback.sh` e verifica a mesma sessão/carrinho nas duas instâncias. O banco/volume não é apagado.

Para uma versão local existente:

```bash
bash scripts/rollback.sh YOUR_KNOWN_GOOD_IMAGE
```

A imagem deve estar disponível localmente e ser compatível com o schema atual. O script troca somente as aplicações, sem repetir migrations. Preserve imagens por SHA e resultados de CI para escolher uma versão verificável.

## Backup e restauração

Docker volumes não são backups. Para SQL Server local, use backup nativo para um arquivo fora do volume da aplicação e confirme a restauração em outra base/instância descartável, inclusive chaves de Data Protection e dados de sessão quando a continuidade for necessária. Proteja o arquivo como dado sensível.

Em Azure SQL, configure/verifique retenção de backups e restaure para um banco novo pelo procedimento de point-in-time restore. Aponte uma instância de teste para a cópia, execute readiness e fluxo com dados sanitizados; só faça cutover após confirmar consistência. Não sobrescreva o banco original para ensaiar recuperação.

Não há evidência de backup/restauração Azure executados neste PR, nem RPO/RTO medidos. A validação de rollback no CI é da aplicação, não de uma restauração de banco. Registre data, versão, ponto recuperado, duração e resultado depois do exercício real.

## Diagnóstico

- **400:** token antiforgery ausente/inválido, JSON inválido, quantidade/versão fora do contrato.
- **404 ao atualizar item:** ID não pertence à sessão ou já foi removido; não tente acessar o carrinho de outra sessão.
- **409:** rowversion desatualizada ou inclusão concorrente; recarregue o carrinho e tente a ação novamente.
- **Readiness falha:** confira SQL, credencial, migrations e tabela de cache. Não registre a connection string no log.
- **Sessão perdida:** confira cookie Secure/HTTPS, o mesmo ApplicationName, conexão SQL compartilhada, validade do cache (30 minutos de inatividade) e acesso às chaves. Não exponha cookies para depurar.
