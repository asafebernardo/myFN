# myFN

Sistema web de controle financeiro pessoal em **.NET 10 + Blazor Web App**, para substituir a planilha mensal.

## Usar sem instalar nada no PC (recomendado no computador da empresa)

O GitHub Pages **não** serve: o myFN precisa de um servidor .NET. No PC da empresa, abra só o **navegador**.

### 1. GitHub Codespaces (mais simples)

1. Entre em https://github.com/asafebernardo/myFN
2. Clique em **Code → Codespaces → Create codespace on main**  
   ou abra direto: https://codespaces.new/asafebernardo/myFN
3. Espere o ambiente subir (o app inicia sozinho na porta 5081).
4. Em **Ports**, deixe 5081 como **Public** e abra o endereço `*.app.github.dev`.

Isso roda na nuvem da GitHub. O navegador da empresa só acessa o site. Se a empresa bloquear Codespaces, use o Render abaixo.

### 2. Site na nuvem (Render)

1. Crie conta em https://render.com (login com GitHub).
2. **New → Web Service →** este repositório.
3. Runtime **Docker** (já existe `Dockerfile` e `render.yaml`).
4. Deploy. O endereço fica tipo `https://myfn.onrender.com`.

No plano gratuito o serviço pode dormir e o SQLite zera se o disco não for persistente. Para uso contínuo, use um disco/plano pago ou outro host (Railway, Fly.io, uma VPS).

Não há login nesta versão: quem tiver o link entra nos dados.

## Como executar no seu computador

Requisitos: SDK .NET 10 (só se a máquina permitir instalar).

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

Em Development o banco SQLite (`src/MyFn.Web/App_Data/myfn.db`) recebe **dados de exemplo** (salário R$ 3.640, obrigatórias, cartões, parcelamentos e fatura Nubank para conferir). Esses registros têm observação `SEED-DEV`. No Docker/Render o ambiente padrão é Production (banco vazio, sem seed).

A tela **Faturas** compara o valor único cobrado no banco com a soma dos gastos no crédito daquele ciclo.

## Testes

```bash
dotnet test
```

## Arquitetura

Ver `docs/ARQUITETURA.md` e `docs/DECISOES.md`.
