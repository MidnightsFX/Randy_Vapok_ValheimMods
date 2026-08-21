# EquipmentAndQuickSlotsAPI

Consumer-side shim for the EquipmentAndQuickSlots (EAQS) integration API. Typed wrappers over the
mod's `EquipmentAndQuickSlots.API` facade; all calls go through reflection, so your plugin needs
no reference to `EquipmentAndQuickSlots.dll` and keeps working when the mod is absent (calls warn
and no-op).

See `EquipmentAndQuickSlots/docs/API.md` in this repository for the endpoint reference.

## Usage

1. Reference `EquipmentAndQuickSlotsAPI.dll` in your project.
2. Merge it into your plugin with ILRepack so you ship a single dll:

```xml
<ItemGroup>
  <PackageReference Include="ILRepack.Lib.MSBuild.Task" Version="2.0.18.2" />
</ItemGroup>
```

```xml
<!-- ILRepack.targets -->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Target Name="ILRepacker" AfterTargets="Build">
    <ItemGroup>
      <InputAssemblies Include="$(TargetPath)" />
      <InputAssemblies Include="$(OutputPath)EquipmentAndQuickSlotsAPI.dll" />
    </ItemGroup>
    <ILRepack Parallel="true" DebugInfo="true" Internalize="true" InputAssemblies="@(InputAssemblies)" OutputFile="$(TargetPath)" TargetKind="SameAsPrimaryAssembly" LibraryPath="$(OutputPath)" />
  </Target>
</Project>
```

3. Declare the soft dependency and gate on `IsLoaded()`:

```csharp
[BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
public class MyPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        if (!EquipmentAndQuickSlotsAPI.EAQS.IsLoaded())
            return;

        EquipmentAndQuickSlotsAPI.EAQS.AddSlot(
            slotId: "MyPluginBackpack",
            ownerPluginGuid: "my.plugin.guid",
            nameToken: "$myplugin_backpack_slot",
            isValid: item => item.m_shared.m_name == "$item_myplugin_backpack",
            isActive: () => true);
    }
}
```

If you prefer raw reflection instead of the shim, resolve
`Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlots")` and invoke the same
endpoints; every signature uses only primitives, `string`, vanilla types and `System.Func`/`Action`
built from those, with `ref` in place of `out`.
