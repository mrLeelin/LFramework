using System;

namespace LFramework.Runtime
{
    /// <summary>
    /// Marks a MonoBehaviour class for automatic dependency injection when its GameObject is processed
    /// by FrameworkGameObjectInjector.InjectGameObjectValidated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class MonoBehaviourValidationAttribute : Attribute
    {
    }
}
