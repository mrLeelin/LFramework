# FAQ

## The app says `ProjectSettingSelector not found`

Initialize the project settings from the framework settings entry so the selector asset is created and discoverable at runtime.

## My components do not start

Check:

- the component type exists
- the type name in `allComponentTypes` is correct
- the component derives from `GameFrameworkComponent`
- any required `ComponentSetting` is present

## Zenject injection is null inside a procedure/provider/world

Verify the object is created through the framework lifecycle rather than manually constructed outside the package startup flow.

## Hotfix procedure does not register providers or worlds

Confirm:

- the procedure derives from `HotfixProcedureBase`
- `ProcedureState` matches the `[BelongTo]` declarations
- hotfix discovery attributes are correctly applied

## YooAsset dependency cannot resolve

Make sure the consuming project has a scoped registry that serves `com.tuyoogame.yooasset`.

## Can I use only part of the package?

Yes, but the package is currently organized as an integrated framework. Partial adoption is easiest when you treat `LSystemApplicationBehaviour`, procedures, providers, and worlds as the main extension model.
