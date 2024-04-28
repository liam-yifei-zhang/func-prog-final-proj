FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY ./FuncProgFinalProj/*.fsproj ./FuncProgFinalProj/
RUN dotnet restore ./FuncProgFinalProj/FuncProgFinalProj.fsproj

COPY . ./
RUN dotnet publish ./FuncProgFinalProj/FuncProgFinalProj.fsproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "FuncProgFinalProj.dll"]

#docker build -t funcprogfinalproj .
#docker run -d --name myfuncapp funcprogfinalproj