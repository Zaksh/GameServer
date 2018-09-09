namespace GameServerCore.Domain
{
    public interface IInventoryManager
    {
        IItem GetItem(int slot);
        void RemoveItem(int slot);
        byte GetItemSlot(IItem item);
        IItem SetExtraItem(byte slot, IItemData item);
        void SwapItems(int slot1, int slot2);
    }
}
