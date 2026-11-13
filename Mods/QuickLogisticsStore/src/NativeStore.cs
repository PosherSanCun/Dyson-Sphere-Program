using System;
using System.Collections.Generic;

namespace SanCun.QuickLogisticsStore
{
    internal sealed class StoreResult
    {
        internal string Message;
        internal long Count;
        internal int Types;
        internal StoreResult(string message) { Message = message; }
    }

    internal static class NativeStore
    {
        internal static StoreResult Execute(UIInventoryWindow window, Player player)
        {
            if (player.deliveryPackage == null || !player.deliveryPackage.unlocked || player.deliveryPackage.activeCount <= 0)
                return new StoreResult("尚未解锁机甲物流背包");
            UIPlayerDeliveryPanel panel = window.deliveryPanel;
            // 原生 UI 在关闭时会释放代理仓，必须核实当前玩家和两个 UI 容器仍保持绑定。
            if (!window.active || !window.inited || window.inventory == null || !window.inventory.inited ||
                panel == null || !panel.active || !panel.inited || panel.deliveryBox == null || !panel.deliveryBox.inited ||
                !ReferenceEquals(window.player, player) || !ReferenceEquals(panel.player, player) ||
                !ReferenceEquals(panel.deliveryPackage, player.deliveryPackage) ||
                !ReferenceEquals(window.inventory.data, player.package))
                return new StoreResult("请重新打开背包后收纳");
            StorageComponent source = player.package;
            StorageComponent destination = panel.deliveryBox.data as StorageComponent;
            if (source == null || source.grids == null || destination == null || destination.grids == null ||
                destination.type != EStorageType.DeliveryFiltered || destination.size != player.deliveryPackage.activeCount)
                return new StoreResult("物流背包尚未就绪，请重新打开背包");

            // 同步方向为真实物流背包→界面代理；原生转移会用代理的回调写回真实 count/inc。
            panel.SyncDeliveryStorage();
            Dictionary<int, long> before = CountItems(source);
            bool configured = false;
            bool matches = false;
            bool hasSpace = false;
            for (int index = 0; index < destination.size; index++)
            {
                StorageComponent.GRID grid = destination.grids[index];
                if (grid.filter <= 0) continue;
                configured = true;
                long available;
                if (!before.TryGetValue(grid.filter, out available) || available <= 0) continue;
                matches = true;
                if (grid.count < grid.stackSize) hasSpace = true;
            }
            if (!configured) return new StoreResult("请先为物流格设置物品");
            if (!matches) return new StoreResult("背包中没有可收纳到物流的物品");
            if (!hasSpace) return new StoreResult("对应的物流格已满，物品留在背包中");

            // 复用原版取出、按筛选收货、余量退回、增产点分配、界面通知和音效，避免自写扣货事务。
            window.inventory.TransferToOther(panel.deliveryBox, player);
            Dictionary<int, long> after = CountItems(source);
            long moved = 0;
            int types = 0;
            foreach (KeyValuePair<int, long> item in before)
            {
                long remaining;
                after.TryGetValue(item.Key, out remaining);
                if (remaining >= item.Value) continue;
                moved += item.Value - remaining;
                types++;
            }
            if (moved == 0) return new StoreResult("物流格未接收物品，物品留在背包中");
            return new StoreResult("已收纳到物流：" + types + " 种，共 " + moved + " 件") { Count = moved, Types = types };
        }

        private static Dictionary<int, long> CountItems(StorageComponent storage)
        {
            Dictionary<int, long> result = new Dictionary<int, long>();
            for (int index = 0; index < storage.grids.Length; index++)
            {
                StorageComponent.GRID grid = storage.grids[index];
                if (grid.itemId <= 0 || grid.count <= 0) continue;
                long count;
                result.TryGetValue(grid.itemId, out count);
                result[grid.itemId] = count + grid.count;
            }
            return result;
        }
    }
}
