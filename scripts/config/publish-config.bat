@echo off
REM Publish Configuration for DocuFiller Update Server
REM Update these values according to your environment

REM Update Server URL (Go server address)
set UPDATE_SERVER_URL=http://localhost:8080

REM Upload Token for authentication (must match Go server config)
set UPDATE_SERVER_TOKEN=change-this-token-in-production

REM Default release channel (stable or beta)
set DEFAULT_CHANNEL=stable
