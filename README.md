# myFN

Sistema web de controle financeiro pessoal em **.NET 10 + Blazor Web App**, para substituir a planilha mensal.

## Como executar

Requisitos: SDK .NET 10.

### Neste computador e pelo IP da rede

O app escuta em **todas as interfaces** na porta **5081** (`http://0.0.0.0:5081`), não só em localhost.

```bash
chmod +x scripts/start-public.sh
./scripts/start-public.sh
```

Ou:

```bash
dotnet restore
dotnet run --project src/MyFn.Web --launch-profile http
```

No terminal aparece o endereço. Exemplos:

- neste PC: `http://127.0.0.1:5081`
- no celular/outro PC da mesma Wi‑Fi: `http://192.168.x.x:5081` (o IP local da máquina)

### Pela internet (IP público)

1. Rode o comando acima e deixe o programa aberto.
2. No roteador, encaminhe a porta **5081 TCP** para o IP local deste computador.
3. Descubra o IP público (ex.: https://ifconfig.me) e abra `http://SEU_IP:5081`.

Não há login nesta versão. Só libere a porta na internet se a rede for sua e você aceitar o risco.

### Docker

```bash
docker compose up --build
```

Acesse `http://IP-DA-MAQUINA:5081`. Os dados ficam no volume `myfn-data`.

Em Development o banco SQLite (`src/MyFn.Web/App_Data/myfn.db`) recebe **dados de exemplo** (salário R$ 3.640, obrigatórias, cartões, parcelamentos e fatura Nubank para conferir). Esses registros têm observação `SEED-DEV`. No Docker o ambiente padrão é Production (banco vazio, sem seed).

A tela **Faturas** compara o valor único cobrado no banco com a soma dos gastos no crédito daquele ciclo.

## Testes

```bash
dotnet test
```

## Arquitetura

Ver `docs/ARQUITETURA.md` e `docs/DECISOES.md`.
