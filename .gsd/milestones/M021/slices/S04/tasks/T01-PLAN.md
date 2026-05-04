---
estimated_steps: 4
estimated_files: 2
skills_used:
  - verify-before-complete
---

# T01: Audit IDocumentProcessor completeness and fix gaps

**Slice:** S04 — 服务接口补全 + CleanupService 日志补充
**Milestone:** M021

## Description

Perform a method-by-method comparison between IDocumentProcessor interface and DocumentProcessorService public API. If any public method/event on the service is missing from the interface, add it. If the interface has stale members not on the service, flag for removal. Verify signatures match exactly (parameter types, return types, default values). Run dotnet build to confirm no compile errors.

## Steps

1. Read `Services/Interfaces/IDocumentProcessor.cs` and extract all member signatures (methods, events, properties)
2. Read `Services/DocumentProcessorService.cs` and extract all public member signatures (excluding constructor and Dispose from IDisposable)
3. Compare the two sets: any method on the service not in the interface → add to interface; any method in the interface not on the service → flag as stale
4. If changes were made, run `dotnet build` to verify compilation. If no changes needed, document that the interface is already complete.

## Must-Haves

- [ ] Every public method/event on DocumentProcessorService (excluding constructor, Dispose) has a matching member in IDocumentProcessor
- [ ] Signatures match exactly (parameter types, return types, default values, generic parameters)
- [ ] dotnet build passes with 0 errors

## Verification

- `dotnet build --no-restore 2>&1 | tail -3` — expect 0 errors
- `grep -c 'public.*(' Services/DocumentProcessorService.cs` — count service methods
- `grep -c 'Task\|void\|event' Services/Interfaces/IDocumentProcessor.cs` — count interface members
- Counts should match (7 methods/events in both)

## Inputs

- `Services/Interfaces/IDocumentProcessor.cs` — current interface definition to audit
- `Services/DocumentProcessorService.cs` — service implementation to compare against

## Expected Output

- `Services/Interfaces/IDocumentProcessor.cs` — interface with any missing members added (or confirmed unchanged if already complete)
