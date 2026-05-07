---
mode: ask
description: Generate a conventional commit message for staged changes
---

# Commit Message Generator

Generate a conventional commit message for the staged changes in this repository.

## Format
```
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

## Types
| Type | When to use |
|------|-------------|
| `feat` | New feature, new screen, new service |
| `fix` | Bug fix |
| `refactor` | Code restructure without behavior change |
| `style` | XAML layout changes, styling only |
| `docs` | Documentation updates (MD files, XML comments) |
| `test` | Adding or updating tests |
| `chore` | DI registration, project file changes, build config |
| `perf` | Performance improvement |

## Scopes (use the feature or layer name)
- `waitlist` · `dashboard` · `mobile`
- `core` · `services` · `data` · `shared`
- `winui` · `droid`
- `di` · `xaml` · `nav`

## Examples
```
feat(waitlist): add approve entry command to ViewModel_Waitlist_Entry

Implements IRelayCommand via [RelayCommand] on ApproveEntryAsync.
Calls IService_WaitlistEntry.ApproveAsync and reloads entries on success.
```

```
chore(di): register Service_WaitlistEntry and Repository_WaitlistEntry

Added Singleton registrations in AddSharedServices() under the
Services and Repositories section headers.
```

```
fix(winui): suppress WMC1006 false-positive in WinUI csproj

Added $(NoWarn);WMC1006 to suppress Windows Metadata compiler warning
caused by the shared project DLL resolution during non-Windows builds.
```

## Rules
- Subject line max 72 characters
- Subject line does not end with a period
- Use imperative mood ("add" not "added" or "adds")
- Body explains *why*, not *what*
- Reference assumption files if applicable: `See .github/assumptions/...`