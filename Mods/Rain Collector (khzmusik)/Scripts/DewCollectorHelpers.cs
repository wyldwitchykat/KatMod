using System;

namespace RainCollector
{
    /// <summary>
    /// Helper methods to handle the dew collector.
    /// </summary>
    public static class DewCollectorHelpers
    {
        /// <summary>
        /// Changes the container size of the <see cref="TileEntityDewCollector"/>.
        /// </summary>
        /// <param name="dewCollector"></param>
        /// <param name="containerSize"></param>
        public static void ChangeContainerSize(
            TileEntityDewCollector dewCollector,
            Vector2i containerSize)
        {
            if (dewCollector == null)
            {
                return;
            }

            if (dewCollector.containerSize == containerSize)
            {
                return;
            }

            // We want to print this message even if we're not debugging
            Log.Out($@"({dewCollector.localChunkPos
                }) Changing container size from {dewCollector.containerSize
                } to {containerSize}");

            // Store the "old" (current) values so we can use them after resetting everything
            var oldItems = dewCollector.items;
            var oldIndex = dewCollector.CurrentIndex;
            var oldFillValue = oldIndex >= 0 ? dewCollector.fillValues[oldIndex] : 0f;

            dewCollector.SetContainerSize(containerSize);

            CopyItemStacks(dewCollector.items, oldItems);

            SetCurrentIndex(dewCollector);

            SetFillValues(dewCollector, oldFillValue);

            return;
        }

        /// <summary>
        /// Changes the container size of the <see cref="TileEntityDewCollector"/> to match the
        /// columns and rows of the container's <see cref="XUiV_Grid"/> view component in the
        /// <see cref="XUiC_DewCollectorContainer"/>.
        /// </summary>
        /// <param name="dewCollector"></param>
        /// <param name="xuicContainer"></param>
        public static void ChangeContainerSize(
            TileEntityDewCollector dewCollector,
            XUiC_DewCollectorContainer xuicContainer)
        {
            if (dewCollector == null)
            {
                return;
            }

            if (!(xuicContainer.viewComponent is XUiV_Grid gridComponent))
            {
                return;
            }

            if (dewCollector.containerSize.x == gridComponent.columns &&
                dewCollector.containerSize.y == gridComponent.rows)
            {
                return;
            }

            ChangeContainerSize(
                dewCollector,
                new Vector2i(gridComponent.columns, gridComponent.rows));
        }

        public static bool IsFull(TileEntityDewCollector dewCollector)
        {
            if (dewCollector.items == null || dewCollector.items.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < dewCollector.items.Length; i++)
            {
                if (dewCollector.items[i].IsEmpty())
                    return false;
            }

            return true;
        }

        private static void CopyItemStacks(ItemStack[] items, ItemStack[] oldItems)
        {
            for (int i = 0, j = 0; i < oldItems.Length && j < items.Length; i++)
            {
                if (!oldItems[i].IsEmpty())
                {
                    items[j++] = oldItems[i].Clone();
                }
            }
        }

        private static void SetCurrentIndex(TileEntityDewCollector dewCollector)
        {
            // The new current index will be the index of the stack BEFORE the first empty
            // item stack - unless the first item stack is empty, or none are empty
            var currentIndex = Array.FindIndex(dewCollector.items, stack => stack.IsEmpty());
            if (currentIndex > 0)
                currentIndex--;

            dewCollector.CurrentIndex = currentIndex;
        }

        private static void SetFillValues(TileEntityDewCollector dewCollector, float oldFillValue)
        {
            // Create new fill values and copy the old fill value to the current index
            // fillValues is actually a property, not a field, and the getter creates the
            // private backing field on demand using the container size
            dewCollector.fillValues = null;

            if (oldFillValue <= 0)
            {
                return;
            }

            if (dewCollector.CurrentIndex >= 0)
            {
                dewCollector.fillValues[dewCollector.CurrentIndex] = oldFillValue;
            }
            else
            {
                // There's no place to put the old fill value so add to the leftover time
                dewCollector.leftoverTime += oldFillValue;
            }
        }
    }
}
