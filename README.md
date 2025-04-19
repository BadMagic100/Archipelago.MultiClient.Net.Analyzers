# Archipelago.MultiClient.Net.Analyzers

Source analyzers, fixes, and code generation for the [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
library.

## Analyzers and Diagnostics

### MULTICLIENT001 - DataStorageElement assigned outside of DataStorageHelper

This warning is intended to prevent bugs which may be caused by misuse of `DataStorageHelper`. The Archipelago
network protocol allows batching several data storage operations in an atomic fashion. In order to support this,
`DataStorageHelper` only sends a packet to the server when a `DataStorageElement`, containing the batched operations,
is re-assigned back to the `DataStorageHelper`. This means that storing a `DataStorageElement` into a variable can
cause undesirable side effects on the element, as well as prevent it from being sent to the server at all. Instead,
it is recommended to use a compound assignment operator to apply edits directly to the `DataStorageHelper`.

This analyzer also offers a corresponding fix action "Make DataStorage access inline" on variable declarations, which
will remove the offending declaration and inline it at all usage sites.

**Incorrect Code:**

```cs
// MULTICLIENT001
DataStorageElement elem = session.DataStorage[Scope.Slot, "MyData"];
elem.Initialize(0);
// This will never make it to the server
elem += 2;
```

**Fixed Code:**

```cs
session.DataStorage[Scope.Slot, "MyData"].Initialize(0);
// This will make it to the server
session.DataStorage[Scope.Slot, "MyData"] += 2;
```

### MULTICLIENT002 - Use HasFlag when comparing ItemFlags

This warning is intended to prevent bugs when comparing `ItemFlags`. Because item classification is a flag,
an item might have multiple flag values set, such as `ItemFlags.Advancement | ItemFlags.Trap`. In such scenarios,
a comparison like `item.Flags == ItemFlags.Advancement` does not capture the programmer's intent ("is this item
a progression item"). Instead, `HasFlag` should be used to perform the comparison. `ItemFlags.Filler` is exempt
from this rule because it has the value 0 and `HasFlag(0)` always returns true. This analyzer also allows using
bitwise AND (e.g. `(item.Flags & ItemFlags.Advancement) == ItemFlags.Advancement`) directly.

This analyzer also offers a corresponding fix action "Use flag comparison" on offending comparisons that contain a constant
on exactly one side of the comparison. These comparisons will be replaced with a matching `HasFlag` check. On older versions
of .NET where `HasFlag` does not exist, a comparison using bitwise AND will be generated instead.

**Incorrect Code:**

```cs
// MULTICLIENT002
return item.Flags == ItemFlags.Advancement;
```

**Fixed Code:**

```cs
return item.Flags.HasFlag(ItemFlags.Advancement);
```

### MULTICLIENT003 - Avoid value comparisons for ItemFlags objects in switch cases

This warning is intended to prevent bugs when comparing `ItemFlags`. Because item classification is a flag,
an item might have multiple flag values set, such as `ItemFlags.Advancement | ItemFlags.Trap`. In such scenarios,
a switch statement does not capture the programmer's intent ("is this item a progression item") due to its use of
direct comparisons. Instead, pattern matching case statements with `HasFlag` should be used to perform the comparison.

This analyzer also offers a corresponding fix action "Convert case to use flag comparison" on offending switch
statements. These statements will be replaced with pattern matching case statements containing the matching `HasFlag` checks.
On older versions of .NET where `HasFlag` does not exist, a comparison using bitwise AND will be generated instead.

**Incorrect Code:**

```cs
ItemFlags itemFlag = ItemFlags.Advancement | ItemFlags.Trap;

// MULTICLIENT003
switch (itemFlag) 
{
    case ItemFlags.Trap:
    case ItemFlags.Advancement:
        return true;
    default:
        return false;
}
```

**Fixed Code:**

```cs
ItemFlags i = ItemFlags.Advancement;
switch (i)
{
    case ItemFlags f when f.HasFlag(ItemFlags.Advancement):
    case ItemFlags f when f1.HasFlag(ItemFlags.Trap):
        return true;
    default:
        return false;
}
```

## Source Generators

### Data Storage Properties

Due to the verbosity of `DataStorageHelper`'s API, it is commonly desirable to assign a `DataStorageElement` to a variable 
for repeated access. Unfortunately, this does not work for most use cases, and there is [an analyzer](#multiclient001---datastorageelement-assigned-outside-of-datastoragehelper)
to prevent misuse of the API. This package offers a source generator to create a thin wrapper around the data storage API
which is also considered an acceptable use by the MULTICLIENT001 analyzer. Note in the example below that you must have a
session defined in a scope that is available to members of the containing class - again, this is only a thin wrapper so you
have bring your own session.

**Example Usage:**

```cs
partial class MyClass
{
    private ArchipelagoSession session;

    [DataStorageProperty(nameof(session), Scope.Slot, "MyScopedData")]
    private readonly DataStorageElement _myScopedData;

    [DataStorageProperty(nameof(session), "MyGlobalData")]
    private readonly DataStorageElement _myGlobalData;

    public void DoStuff()
    {
        MyScopedData.Initialize(0);
        MyGlobalData += 2;
    }
}
```