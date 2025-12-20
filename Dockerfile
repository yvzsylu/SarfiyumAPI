# --------------------------------------------------------
# 1. AÞAMA: Frontend Build (Angular)
# --------------------------------------------------------
FROM node:20-alpine as node-build
WORKDIR /src

# Baðýmlýlýklarý yükle
COPY ["SarfiyumUI/package.json", "SarfiyumUI/package-lock.json", "./SarfiyumUI/"]
WORKDIR /src/SarfiyumUI
RUN npm install --legacy-peer-deps

# ÖNEMLÝ: Önce kodlarý kopyalamalýyýz ki environment dosyasý orada olsun
COPY SarfiyumUI/ .

# Dosyalarý da ne olur ne olmaz Prod ile eþitleyelim
RUN cp src/environments/environment.prod.ts src/environments/environment.ts

# Build al
RUN npm run build:prod

# --------------------------------------------------------
# 2. AÞAMA: Backend Build (.NET 8)
# --------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src

# Sadece içeriði deðil, tüm klasör yapýsýný kopyalýyoruz
COPY . .

# Artýk hiyerarþi korunduðu için bu yollar doðru çalýþacak
RUN dotnet restore "SarfiyumAPI/SarfiyumAPI.csproj"
RUN dotnet publish "SarfiyumAPI/SarfiyumAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --------------------------------------------------------
# 3. AÞAMA: Final Image (Runtime)
# --------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Backend Dosyalarýný Al
COPY --from=dotnet-build /app/publish .

# Frontend Dosyalarýný Al (Angular 19 yolu: /browser)
COPY --from=node-build /src/SarfiyumUI/dist/sarfiyum/browser ./wwwroot

# Uygulama Ayarlarý
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Uygulamayý Baþlat
ENTRYPOINT ["dotnet", "SarfiyumAPI.dll"]
