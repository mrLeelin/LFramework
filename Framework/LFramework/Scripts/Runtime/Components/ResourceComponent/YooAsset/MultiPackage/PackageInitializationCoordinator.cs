using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace LFramework.Runtime
{
    /// <summary>
    /// Coordinates package initialization requests so concurrent callers share the same in-flight operation.
    /// </summary>
    public sealed class PackageInitializationCoordinator
    {
        private readonly Func<string, bool> _isPackageReady;
        private readonly Func<string, UniTask<PackageInitializationResult>> _initializeAsync;
        private readonly Dictionary<string, UniTask<PackageInitializationResult>> _inflight =
            new Dictionary<string, UniTask<PackageInitializationResult>>(StringComparer.Ordinal);

        /// <summary>
        /// Creates a package initialization coordinator.
        /// </summary>
        public PackageInitializationCoordinator(
            Func<string, bool> isPackageReady,
            Func<string, UniTask<PackageInitializationResult>> initializeAsync)
        {
            _isPackageReady = isPackageReady ?? throw new ArgumentNullException(nameof(isPackageReady));
            _initializeAsync = initializeAsync ?? throw new ArgumentNullException(nameof(initializeAsync));
        }

        /// <summary>
        /// Ensures the target package is ready for loading.
        /// </summary>
        public UniTask<PackageInitializationResult> EnsureInitializedAsync(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return UniTask.FromResult(PackageInitializationResult.CreateFailure(packageName, "Package name is empty."));
            }

            if (_isPackageReady(packageName))
            {
                return UniTask.FromResult(PackageInitializationResult.CreateSuccess(packageName));
            }

            if (_inflight.TryGetValue(packageName, out UniTask<PackageInitializationResult> task))
            {
                return task;
            }

            var source = new UniTaskCompletionSource<PackageInitializationResult>();
            _inflight[packageName] = source.Task;
            RunInitializationAsync(packageName, source).Forget();
            return source.Task;
        }

        private async UniTaskVoid RunInitializationAsync(string packageName, UniTaskCompletionSource<PackageInitializationResult> source)
        {
            try
            {
                PackageInitializationResult result = await _initializeAsync(packageName);
                source.TrySetResult(result);
            }
            catch (Exception ex)
            {
                source.TrySetResult(PackageInitializationResult.CreateFailure(packageName, ex.Message));
            }
            finally
            {
                _inflight.Remove(packageName);
            }
        }
    }

    /// <summary>
    /// Captures the result of a single package initialization attempt.
    /// </summary>
    public readonly struct PackageInitializationResult
    {
        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static PackageInitializationResult CreateSuccess(string packageName)
        {
            return new PackageInitializationResult(packageName, true, null);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static PackageInitializationResult CreateFailure(string packageName, string errorMessage)
        {
            return new PackageInitializationResult(packageName, false, errorMessage);
        }

        /// <summary>
        /// The physical YooAsset package name.
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// Whether initialization succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The failure message when initialization does not succeed.
        /// </summary>
        public string ErrorMessage { get; }

        private PackageInitializationResult(string packageName, bool succeeded, string errorMessage)
        {
            PackageName = packageName;
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }
    }
}
