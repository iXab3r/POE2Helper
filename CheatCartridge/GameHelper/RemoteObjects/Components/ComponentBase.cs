using CheatCartridge.GameHelper.GameOffsets.Objects.Components;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects.Components;

/// <summary>
///     Component base object that contains component owner entity address.
///     All components in the game have this.
/// </summary>
public class ComponentBase : MemoryObjectBase
{
    public ComponentBase(IMemory memory, IntPtr address) :
        base(memory, address)
    {
    }

    /// <summary>
    ///     Owner entity address of this component.
    /// </summary>
    protected IntPtr OwnerEntityAddress;

    protected override void CleanUpData()
    {
        throw new InvalidOperationException("Component Address should never be Zero.");
    }

    protected override void UpdateData(bool hasAddressChanged)
    {
        var data = Memory.Read<ComponentHeader>(Address);
        OwnerEntityAddress = data.EntityPtr;
    }

    /// <summary>
    ///     Validate if the component is pointing to parent entity address or not
    /// </summary>
    /// <param name="parentEntityAddress">true if component is pointing to parent entity address otherwise false</param>
    /// <returns></returns>
    public bool IsParentValid(IntPtr parentEntityAddress)
    {
        return OwnerEntityAddress == parentEntityAddress;
    }
}