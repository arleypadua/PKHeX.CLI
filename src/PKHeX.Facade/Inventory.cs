using System.Collections;
using System.Collections.Immutable;
using PKHeX.Facade.Repositories;
using PKHeX.Core;

namespace PKHeX.Facade;

public class Inventory : IEnumerable<Inventory.Item>
{
    /// <summary>
    /// The shared target count requested by facade item editors and bulk fill operations.
    /// Save formats may store a lower count according to their native rules.
    /// </summary>
    public const int ItemTargetCount = 900;

    private readonly Game _game;
    private readonly PlayerBag _bag;
    private readonly InventoryPouch _pouch;

    public Inventory(string type, Game game)
    {
        _game = game;
        _bag = _game.SaveFile.Inventory;
        _pouch = _bag.Pouches.FirstOrDefault(i => i.Type.ToString() == type)
            ?? throw new InvalidOperationException($"Inventory of type {type} not found");

        Type = type;
    }

    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
    public IEnumerator<Item> GetEnumerator() => Items.GetEnumerator();


    public string Type { get; init; }
    public ImmutableList<Item> Items => GetItems();

    /// <summary>
    /// Returns the max count a single item can have
    /// </summary>
    public int MaxItemCountAllowed => _pouch.MaxCount;

    /// <summary>
    /// Returns a list of all items supported in this inventory
    /// </summary>
    public ImmutableList<ItemDefinition> AllSupportedItems => _pouch
        .GetAllItems()
        .ToArray()
        .Select(_game.ItemRepository.GetGameItem).ToImmutableList();

    /// <summary>
    /// Returns a list of all items that can be added to the current inventory
    /// </summary>
    public ImmutableList<ItemDefinition> CurrentSupportedItems => AllSupportedItems
        .Except(Items.Select(i => i.Definition))
        .ToImmutableList();

    public bool Supports(ItemDefinition item) =>
        AllSupportedItems.Any(i => i.Id == item.Id);

    public void Remove(ushort itemId)
    {
        if (itemId == ItemDefinition.None)
        {
            throw new InvalidOperationException("Cannot remove None item.");
        }

        _pouch.RemoveAll(i => i.Index == itemId);
        Commit();
    }

    public void Set(ushort itemId, uint count)
    {
        if (itemId == ItemDefinition.None)
        {
            throw new InvalidOperationException("Cannot set item to None.");
        }

        if (AllSupportedItems.All(i => i.Id != itemId))
        {
            throw new InvalidOperationException($"Item {itemId} is not supported in this inventory.");
        }

        _pouch.RemoveAll(i => i.Index == itemId);
        if (count > 0)
            _pouch.GiveItem(_bag, itemId, Convert.ToInt32(count));

        Commit();
    }

    private void Commit()
    {
        _bag.CopyTo(_game.SaveFile);
    }

    private ImmutableList<Item> GetItems()
    {
        return _pouch.Items.Select(i => new Item(i, _game.ItemRepository.GetGameItem)).ToImmutableList();
    }

    public sealed class Item
    {
        private readonly Func<ushort, ItemDefinition> _itemFetcher;
        private readonly InventoryItem _item;

        public Item(InventoryItem item, Func<ushort, ItemDefinition> itemFetcher)
        {
            _itemFetcher = itemFetcher;
            _item = item;
        }

        public Item() : this(default!, default!) { }

        public ushort Id => Convert.ToUInt16(_item.Index);
        public string Name => _itemFetcher(Id).Name;
        public int Count => _item.Count;

        public bool IsNone => Id == ItemDefinition.None;
        public ItemDefinition Definition => _itemFetcher(Id);

        public override string ToString() => $"{Name} x{Count}";
    }
}
