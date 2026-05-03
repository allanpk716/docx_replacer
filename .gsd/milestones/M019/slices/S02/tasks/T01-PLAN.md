---
estimated_steps: 24
estimated_files: 3
skills_used: []
---

# T01: Generate app.ico and configure ApplicationIcon in csproj

Use a Python/Pillow script to programmatically generate a DocuFiller application icon (256x256 PNG converted to multi-resolution .ico). The icon should represent a Word document with a fill/overlay symbol, using professional colors. Then configure the csproj with `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` and ensure the Resources directory exists with the icon file. Also add the icon as a WPF Resource so it can be referenced from XAML via pack URI.

## Steps

1. Create `Resources/` directory in the project root
2. Write a Python script that uses Pillow to generate a professional app icon:
   - Size: 256x256 (will be saved as multi-resolution .ico with 16, 32, 48, 64, 128, 256)
   - Design: A stylized Word document (blue rectangle with folded corner) with a green checkmark or fill indicator
   - Colors: Professional blue (#2B579A for document body), lighter blue (#4A90D9 for accent), green (#4CAF50 for checkmark)
   - Background: Transparent
3. Run the script to generate `Resources/app.ico`
4. Also save a `Resources/app.png` (256x256) for use in XAML Image controls
5. Edit `DocuFiller.csproj`: Add `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` inside the main `<PropertyGroup>` (after the existing properties like `<Version>`)
6. Run `dotnet build` to verify the icon embeds into the exe without errors
7. Verify the exe has the icon by checking file properties (optional, manual)

## Must-Haves

- [ ] `Resources/app.ico` exists with multi-resolution icon (16-256px)
- [ ] `Resources/app.png` exists (256x256 PNG with transparency)
- [ ] `DocuFiller.csproj` contains `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` in PropertyGroup
- [ ] `dotnet build` succeeds with 0 errors

## Verification

- `python -c "from PIL import Image; img = Image.open('Resources/app.ico'); print(f'Icon sizes: {img.info.get(\"sizes\", \"unknown\")}')"` shows multiple sizes
- `powershell -Command "Select-String -Path DocuFiller.csproj -Pattern 'ApplicationIcon'"` returns matching line
- `dotnet build` exits with code 0

## Observability Impact

No runtime observability changes — this is a static resource task.

## Inputs

- `DocuFiller.csproj`

## Expected Output

- `Resources/app.ico`
- `Resources/app.png`
- `DocuFiller.csproj`

## Verification

dotnet build && python -c "from PIL import Image; img=Image.open('Resources/app.ico'); print('OK:', img.size)"
