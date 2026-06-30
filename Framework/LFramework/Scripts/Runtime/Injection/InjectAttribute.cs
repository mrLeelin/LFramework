using System;

namespace LFramework.Runtime
{
    /// <summary>
    /// Framework-owned injection marker.
    /// The runtime dispatcher does not read this attribute; the compile-time source generator uses it as source metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectAttribute : Attribute
    {
        /// <summary>
        /// Optional service identifier for keyed registrations.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// When true, missing services leave the member untouched instead of throwing.
        /// </summary>
        public bool Optional { get; set; }
    }
}
