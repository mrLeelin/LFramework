using System;

namespace GameFramework.UI
{
    /// <summary>
    /// Optional notification capability for UI managers that can cancel unfinished opens.
    /// </summary>
    public interface IUIFormCancellationSource
    {
        /// <summary>
        /// Raised synchronously after an unfinished UI open has been canceled.
        /// </summary>
        event Action<int> OpenUIFormCanceled;
    }
}
