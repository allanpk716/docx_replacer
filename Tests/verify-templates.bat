@echo off
echo Checking test template files...
echo.

if exist "Templates\template-with-header.docx" (
    echo [OK] template-with-header.docx
) else (
    echo [MISSING] template-with-header.docx
)

if exist "Templates\template-with-footer.docx" (
    echo [OK] template-with-footer.docx
) else (
    echo [MISSING] template-with-footer.docx
)

if exist "Templates\template-with-both.docx" (
    echo [OK] template-with-both.docx
) else (
    echo [MISSING] template-with-both.docx
)

if exist "Templates\template-odd-even.docx" (
    echo [OK] template-odd-even.docx
) else (
    echo [MISSING] template-odd-even.docx
)

echo.
echo Test data files:
if exist "Data\test-data.json" (
    echo [OK] test-data.json
) else (
    echo [MISSING] test-data.json
)

echo.
echo Please refer to Templates/README.md for instructions on creating test templates
pause
