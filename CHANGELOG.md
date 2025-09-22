# Changelog

All notable changes to this project will be documented in this file.

## [0.1.4] - 2025-09-22
### Added
- NavMesh related sample adjustments (prepare ground / waiting area so scene works with minimal manual bake steps).
- (If included) Prefab or data ensuring sample queue area is ready on import.
### Changed
- README install link updated to v0.1.4.

## [0.1.3] - 2025-09-22
### Removed
- Duplicate scripts cleaned from package.

## [0.1.2] - 2025-09-22
### Fixed
- Prefab references and missing sample prefab issues corrected (ensured sample scene opens without missing scripts/prefabs).

### Changed
- Documentation still concise; clarified internal sample asset layout.

## [0.1.1] - 2025-09-22
### Fixed
- NavMesh agent type issues in sample scene (invalid agent type). Guidance added: reassign a valid Agent Type & bake.

### Added
- Basic troubleshooting notes internally (NavMesh & TMP expectations).

## [0.1.0] - 2025-09-22
### Added
- Initial public release: ScriptableObject-based Behavior Tree core (Node, Sequence, Selector, DeadlineSequence, Parallel).
- Per-agent deep cloning runtime model.
- Queue System sample (domain separated from core).
- Minimal English README.

### Notes
- Unity version field set to 2022.3 as minimum compatible baseline; tested also on 6000.x.

---
Format: Keep newest on top. Use tags v0.1.x (#tag in git URL). Future: add decorators & live inspector.
