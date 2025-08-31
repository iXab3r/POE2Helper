using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.RemoteObjects;

/// <summary>
///     Points to a Memory location and reads/understands all the data from there.
///     CurrentAreaInstance in remote memory location changes w.r.t time or event. Due to this,
///     each remote memory object requires to implement a time/event based coroutine.
/// </summary>
public abstract class MemoryObjectBase 
{
    private IntPtr address;

    public MemoryObjectBase(IMemory memory)
    {
        Memory = memory;
    }

    public MemoryObjectBase(IMemory memory, IntPtr address) : this(memory)
    {
        Address = address;
    }
    
    public IMemory Memory { get; }

    /// <summary>
    ///     Gets or sets the address of the memory location.
    /// </summary>
    public IntPtr Address
    {
        get => address;
        set
        {
            var hasAddressChanged = address != value;
            if (hasAddressChanged)
            {
                address = value;
                if (value == IntPtr.Zero)
                {
                    CleanUpData();
                }
                else
                {
                    UpdateData(true);
                }
            }
            else if (address != IntPtr.Zero)
            {
                UpdateData(false);
            }
        }
    }

    public void UpdateData()
    {
        UpdateData(hasAddressChanged: false);
    }

    /// <summary>
    ///     Reads the memory and update all the data known by this Object.
    /// </summary>
    /// <param name="hasAddressChanged">
    ///     true in case the address has changed; otherwise false.
    /// </param>
    protected abstract void UpdateData(bool hasAddressChanged);

    /// <summary>
    ///     Knows how to clean up the object.
    /// </summary>
    protected abstract void CleanUpData();
    
    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.AppendParameterIfNotDefault(nameof(Address), Address.ToHexadecimal());
        return builder.ToString();
    }
}

