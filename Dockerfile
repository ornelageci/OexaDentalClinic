FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Backend/OexaDentalClinic.Api/OexaDentalClinic.Api.csproj Backend/OexaDentalClinic.Api/
RUN dotnet restore Backend/OexaDentalClinic.Api/OexaDentalClinic.Api.csproj

COPY Backend/OexaDentalClinic.Api/ Backend/OexaDentalClinic.Api/
RUN dotnet publish Backend/OexaDentalClinic.Api/OexaDentalClinic.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OexaDentalClinic.Api.dll"]
