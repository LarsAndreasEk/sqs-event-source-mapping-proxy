FROM mcr.microsoft.com/dotnet/runtime-deps:6.0

ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /app
ENTRYPOINT ["./Proxy"]
COPY ./published .