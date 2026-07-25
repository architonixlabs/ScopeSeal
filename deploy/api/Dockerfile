FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ScopeSeal.slnx ./
COPY hosts/ScopeSeal.Api/ScopeSeal.Api.csproj hosts/ScopeSeal.Api/
COPY modules/ScopeSeal.Shared/ScopeSeal.Shared.csproj modules/ScopeSeal.Shared/
COPY modules/ScopeSeal.Infrastructure/ScopeSeal.Infrastructure.csproj modules/ScopeSeal.Infrastructure/
COPY modules/ScopeSeal.Identity/ScopeSeal.Identity.csproj modules/ScopeSeal.Identity/
COPY modules/ScopeSeal.Tenancy/ScopeSeal.Tenancy.csproj modules/ScopeSeal.Tenancy/
COPY modules/ScopeSeal.Workspaces/ScopeSeal.Workspaces.csproj modules/ScopeSeal.Workspaces/
COPY modules/ScopeSeal.Documents/ScopeSeal.Documents.csproj modules/ScopeSeal.Documents/
COPY modules/ScopeSeal.AgreementSnapshots/ScopeSeal.AgreementSnapshots.csproj modules/ScopeSeal.AgreementSnapshots/
COPY modules/ScopeSeal.Approvals/ScopeSeal.Approvals.csproj modules/ScopeSeal.Approvals/
COPY modules/ScopeSeal.ChangeLedger/ScopeSeal.ChangeLedger.csproj modules/ScopeSeal.ChangeLedger/
COPY modules/ScopeSeal.Extraction/ScopeSeal.Extraction.csproj modules/ScopeSeal.Extraction/
COPY modules/ScopeSeal.Billing/ScopeSeal.Billing.csproj modules/ScopeSeal.Billing/
COPY modules/ScopeSeal.Entitlements/ScopeSeal.Entitlements.csproj modules/ScopeSeal.Entitlements/
COPY modules/ScopeSeal.Privacy/ScopeSeal.Privacy.csproj modules/ScopeSeal.Privacy/
COPY modules/ScopeSeal.Administration/ScopeSeal.Administration.csproj modules/ScopeSeal.Administration/
COPY modules/ScopeSeal.Audit/ScopeSeal.Audit.csproj modules/ScopeSeal.Audit/
COPY modules/ScopeSeal.Notifications/ScopeSeal.Notifications.csproj modules/ScopeSeal.Notifications/
RUN dotnet restore hosts/ScopeSeal.Api/ScopeSeal.Api.csproj
COPY . .
RUN dotnet publish hosts/ScopeSeal.Api/ScopeSeal.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "ScopeSeal.Api.dll"]
