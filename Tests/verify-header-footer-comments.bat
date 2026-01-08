@echo off
cd /d "%~dp0"
echo Running header-footer comment verification...
dotnet test --filter "FullyQualifiedName~HeaderFooterComment"
echo Done.
pause
