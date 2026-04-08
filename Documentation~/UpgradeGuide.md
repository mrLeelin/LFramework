# Upgrade Guide

## Current versioning posture

`com.lframework.core` is still in the `0.x` stage. Expect improvements to package shape, docs, and API boundaries while the package is being productized for team reuse.

## Recommended upgrade workflow

1. Read `CHANGELOG.md`
2. Compare package dependency requirements
3. Re-import one sample or bootstrap scene in a staging branch
4. Validate:
   - startup
   - DI binding
   - procedure transitions
   - hotfix load path
   - UI/resource initialization

## Breaking-change checklist

When updating to a new version, check for:

- renamed or moved component classes
- changed settings asset expectations
- changed procedure/provider/world registration behavior
- updated dependency versions
- editor tooling path changes

## Team policy suggestion

For internal team rollout:

- upgrade in a framework integration branch first
- run smoke tests before merging
- publish migration notes for app teams

## Future recommendation

When the package reaches `1.x`, keep this document versioned with:

- explicit migration sections
- deprecated API replacements
- known incompatible changes
