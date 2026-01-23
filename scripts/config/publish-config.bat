@echo off
REM Publish Configuration for DocuFiller Update Server
REM This file contains sensitive information (API token)
REM DO NOT commit this file to version control

REM Update Server URL
set UPDATE_SERVER_URL=http://172.18.200.47:58100

REM Upload Token for authentication (must match server config)
set UPDATE_SERVER_TOKEN=change-this-token-in-production

REM Program ID for this application
set PROGRAM_ID=docufiller
