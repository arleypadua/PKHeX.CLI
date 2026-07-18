using System.Collections.Immutable;
using PKHeX.Core;

namespace PKHeX.Facade;

public class Inventories
{
    private readonly Game _game;

    public Inventories(Game game)
    {
        _game = game;

        InventoryTypes = GetInventoryTypes();
        InventoryItems = GetInventories();
    }

    public Inventory this[string key]
    {
        get
        {
            if (InventoryItems.ContainsKey(key))
            {
                return InventoryItems[key];
            }
            else
            {
                throw new KeyNotFoundException($"The inventory '{key}' does not exist.");
            }
        }
    }


    public ImmutableHashSet<string> InventoryTypes { get; private set; }
    public ImmutableDictionary<string, Inventory> InventoryItems { get; private set; }
    public bool HasSupportedKeyItems
    {
        get
        {
            var bag = _game.SaveFile.Inventory;
            return bag.Pouches.Any(pouch =>
                pouch.GetAllItems().ToArray().Any(itemId => IsKeyItem(bag.Info, pouch.Type, itemId)));
        }
    }

    public InventoryFillSummary SetAllSupportedToMax()
        => SetSupportedItemsToMax(keyItemsOnly: false);

    public InventoryFillSummary SetSupportedKeyItemsToMax()
        => SetSupportedItemsToMax(keyItemsOnly: true);

    private InventoryFillSummary SetSupportedItemsToMax(bool keyItemsOnly)
    {
        var bag = _game.SaveFile.Inventory;
        var requestedItemCount = 0;
        var filledItemCount = 0;
        var clampedItemCount = 0;

        foreach (var pouch in bag.Pouches)
        {
            var legalItemIds = pouch.GetAllItems()
                .ToArray()
                .Where(itemId => IsKeyItem(bag.Info, pouch.Type, itemId) == keyItemsOnly)
                .Where(itemId => bag.IsLegal(pouch.Type, itemId, pouch.MaxCount))
                .ToArray();
            requestedItemCount += legalItemIds.Length;
            foreach (var legalItemId in legalItemIds)
            {
                var requestedCount = keyItemsOnly ? pouch.MaxCount : Inventory.ItemTargetCount;
                var existingItem = pouch.Items.FirstOrDefault(item => item.Index == legalItemId);
                var actualCount = existingItem is null
                    ? pouch.GiveItem(bag, legalItemId, requestedCount)
                    : existingItem.Count = bag.Clamp(pouch.Type, legalItemId, requestedCount);
                if (actualCount <= 0)
                    continue;

                filledItemCount++;
                if (actualCount < requestedCount)
                    clampedItemCount++;
            }
        }

        bag.CopyTo(_game.SaveFile);
        InventoryTypes = GetInventoryTypes();
        InventoryItems = GetInventories();

        return new InventoryFillSummary(requestedItemCount, filledItemCount)
        {
            ClampedItemCount = clampedItemCount,
        };
    }

    internal static bool IsKeyItem(IItemStorage itemStorage, InventoryType pouchType, ushort itemId)
    {
        if (pouchType is InventoryType.KeyItems)
            return true;

        return itemStorage switch
        {
            ItemStorage7GG => ItemStorage7GG.Key.Contains(itemId),
            ItemStorage8BDSP => ItemStorage8BDSP.GetInventoryPouch(itemId) is InventoryType.KeyItems,
            ItemStorage9SV => ItemStorage9SV.GetInventoryPouch(itemId) is InventoryType.KeyItems,
            ItemStorage9ZA => ItemStorage9ZA.GetInventoryPouch(itemId) is InventoryType.KeyItems,
            _ => false,
        };
    }

    private ImmutableHashSet<string> GetInventoryTypes()
        => _game.SaveFile.Inventory.Pouches.Select(i => i.Type.ToString()).ToImmutableHashSet();

    private ImmutableDictionary<string, Inventory> GetInventories() => InventoryTypes.ToImmutableDictionary(
        type => type,
        type => new Inventory(type, _game)
    );
}

public static class InventoriesExtensions
{
    public static IEnumerable<Inventory.Item> AllExceptNone(this Inventory inventory)
    {
        return inventory.Where(i => !i.IsNone);
    }
}

public readonly record struct InventoryFillSummary(
    int RequestedItemCount,
    int AddedItemCount)
{
    /// <summary>
    /// The number of requested items with a positive stored count after the fill operation.
    /// </summary>
    public int FilledItemCount => AddedItemCount;

    public int ClampedItemCount { get; init; }
    public int SkippedItemCount => RequestedItemCount - AddedItemCount;
}
