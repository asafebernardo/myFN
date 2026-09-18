#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

PORT="${PORT:-5081}"
export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

lan_ips() {
  if command -v hostname >/dev/null 2>&1; then
    hostname -I 2>/dev/null | tr ' ' '\n' | grep -E '^[0-9]+\.' | grep -v '^127\.' || true
  fi
}

echo "myFN escutando em todas as interfaces na porta ${PORT}."
echo
echo "Neste computador:  http://127.0.0.1:${PORT}"
while read -r ip; do
  [ -n "$ip" ] && echo "Na rede (IP):     http://${ip}:${PORT}"
done < <(lan_ips)
echo
echo "Pela internet: encaminhe a porta ${PORT}/TCP no roteador para este computador"
echo "e abra http://SEU_IP_PUBLICO:${PORT}"
echo "Não há login nesta versão — só exponha a porta se a rede for confiável."
echo

exec dotnet run --project src/MyFn.Web --no-launch-profile
