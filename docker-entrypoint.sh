#!/bin/sh
set -e
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-5081}"
exec dotnet MyFn.Web.dll
