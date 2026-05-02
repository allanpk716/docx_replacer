---
estimated_steps: 20
estimated_files: 9
skills_used: []
---

# T03: Delete JSON editor leftover files and verify final build

Delete the 8 orphaned JSON editor files that have no DI registration and no active references, then run a final build verification to confirm the entire slice is complete.

## Steps

1. Delete the JSON editor service files:
   - `Services/JsonEditorService.cs`
   - `Services/Interfaces/IJsonEditorService.cs`
   - `Services/KeywordValidationService.cs`
   - `Services/Interfaces/IKeywordValidationService.cs`

2. Delete the JSON editor model files:
   - `Models/JsonKeywordItem.cs`
   - `Models/JsonProjectModel.cs`

3. Delete the JSON editor ViewModel and View:
   - `ViewModels/JsonEditorViewModel.cs`
   - `Views/JsonEditorWindow.xaml`
   - `Views/JsonEditorWindow.xaml.cs`

4. Run `dotnet build` and confirm 0 errors

5. Run grep verification to confirm no update or JSON editor code files remain in the project

## Must-Haves

- [ ] All 8+ JSON editor files deleted
- [ ] dotnet build succeeds with 0 errors
- [ ] No remaining update or JSON editor code files in the codebase

## Inputs

- `Services/JsonEditorService.cs`
- `Services/Interfaces/IJsonEditorService.cs`
- `Services/KeywordValidationService.cs`
- `Services/Interfaces/IKeywordValidationService.cs`
- `Models/JsonKeywordItem.cs`
- `Models/JsonProjectModel.cs`
- `ViewModels/JsonEditorViewModel.cs`
- `Views/JsonEditorWindow.xaml`
- `Views/JsonEditorWindow.xaml.cs`

## Expected Output

- `DocuFiller.csproj`

## Verification

test ! -f Services/JsonEditorService.cs; test ! -f Services/Interfaces/IJsonEditorService.cs; test ! -f Models/JsonKeywordItem.cs; test ! -f ViewModels/JsonEditorViewModel.cs; test ! -f Views/JsonEditorWindow.xaml; dotnet build exits with code 0
