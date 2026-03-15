using System.Collections.Generic;
using System.Linq;

namespace Sandblox.Models;

public class Inventory
{
    private const int HotbarSize = 9;
    private const int MainSize = 27;
    private ItemStack?[] _hotbar = new ItemStack?[HotbarSize];
    private ItemStack?[] _main = new ItemStack?[MainSize];

    public ItemStack? GetHotbarItem(int slot) => slot >= 0 && slot < HotbarSize ? _hotbar[slot] : null;

    public void AddItem(ItemStack stack)
    {
        // First try to stack with existing items
        foreach (var inv in new[] { _hotbar, _main })
        {
            for (int i = 0; i < inv.Length; i++)
            {
                if (inv[i] != null && inv[i].Type == stack.Type && inv[i].Count < 64)
                {
                    int space = 64 - inv[i].Count;
                    int add = System.Math.Min(space, stack.Count);
                    inv[i].Count += add;
                    stack.Count -= add;
                    if (stack.Count == 0) return;
                }
            }
        }
        // Then place in empty slot
        foreach (var inv in new[] { _hotbar, _main })
        {
            for (int i = 0; i < inv.Length; i++)
            {
                if (inv[i] == null)
                {
                    inv[i] = stack;
                    return;
                }
            }
        }
        // Drop if full (not implemented)
    }

    public void RemoveItem(ItemStack stack)
    {
        int remaining = stack.Count;
        foreach (var inv in new[] { _hotbar, _main })
        {
            for (int i = 0; i < inv.Length; i++)
            {
                if (inv[i] != null && inv[i].Type == stack.Type)
                {
                    int take = System.Math.Min(inv[i].Count, remaining);
                    inv[i].Count -= take;
                    remaining -= take;
                    if (inv[i].Count == 0) inv[i] = null;
                    if (remaining == 0) return;
                }
            }
        }
    }
}