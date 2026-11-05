namespace CoffinEscape
{
    /// <summary>Pure C# grip-toggle state. Input, selection, haptics, and movement stay outside this type.</summary>
    public sealed class GrabToggleLogic<T> where T : class
    {
        public enum Result
        {
            None,
            Grab,
            Release,
            IgnoredDuplicateFrame,
        }

        private T heldItem;
        private int lastPressFrame = int.MinValue;

        public T HeldItem => heldItem;
        public bool IsHolding => heldItem != null;

        public Result Press(T closestCandidate, int frame, out T affectedItem)
        {
            affectedItem = null;
            if (frame == lastPressFrame)
                return Result.IgnoredDuplicateFrame;

            lastPressFrame = frame;
            if (heldItem != null)
            {
                affectedItem = heldItem;
                heldItem = null;
                return Result.Release;
            }

            if (closestCandidate == null)
                return Result.None;

            heldItem = closestCandidate;
            affectedItem = closestCandidate;
            return Result.Grab;
        }

        public bool CancelGrab(T item)
        {
            if (!ReferenceEquals(heldItem, item))
                return false;

            heldItem = null;
            return true;
        }

        public void Reset() => heldItem = null;
    }
}
