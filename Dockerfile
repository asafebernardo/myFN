FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/MyFn.Web/MyFn.Web.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5081
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5081
VOLUME ["/app/App_Data"]
ENTRYPOINT ["dotnet", "MyFn.Web.dll"]
