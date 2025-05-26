using System;
using UnityEngine;

public class CursorStack
{
    public static CursorStack Instance { get; } = new CursorStack();
    private ItemStack _stack;
    public ItemStack ItemStack
    {
        get => _stack;
        set
        {
            if (!_stack.Equals(value))
            {
                _stack = value;
                OnCursorChanged?.Invoke(_stack);
            }
        }
    }

    public int Amount
    {
        get => _stack.amount;
        set
        {
            ItemStack = new ItemStack { Id = _stack.Id, amount = value };
        }
    }
    //fired whenever this stack changes
    public event Action<ItemStack> OnCursorChanged;
}
