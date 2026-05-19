# ----- Frontend build -----
FROM node:20 AS frontend
WORKDIR /app/frontend
COPY frontend .
RUN npm install
RUN npm run build

# ----- Backend build -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY backend .
COPY --from=frontend /app/frontend/build ./TriviaBackend/wwwroot
RUN ls ./TriviaBackend/wwwroot
WORKDIR /app/TriviaBackend
RUN dotnet publish -c Release -o /app/out

# ----- Final runtime -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend-build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "TriviaBackend.dll"]
